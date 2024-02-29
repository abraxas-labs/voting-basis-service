// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Core.Utils;
using Voting.Basis.EventSignature;
using Voting.Basis.EventSignature.Models;
using Voting.Lib.Common;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using ProtoEventSignatureBusinessMetadata = Abraxas.Voting.Basis.Events.V1.Metadata.EventSignatureBusinessMetadata;

namespace Voting.Basis.Core.EventSignature;

/// <summary>
/// A service which handles event signature operations.
/// The methods are not thread-safe.
/// </summary>
public class EventSignatureService
{
    private readonly ILogger<EventSignatureService> _logger;
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;
    private readonly IPkcs11DeviceAdapter _pkcs11DeviceAdapter;
    private readonly IEventSerializer _eventSerializer;
    private readonly IClock _clock;
    private readonly IServiceProvider _serviceProvider;
    private readonly MachineConfig _machineConfig;
    private readonly IMapper _mapper;
    private readonly ContestCache _contestCache;

    public EventSignatureService(
        ILogger<EventSignatureService> logger,
        IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> asymmetricAlgorithmAdapter,
        IPkcs11DeviceAdapter pkcs11DeviceAdapter,
        ContestCache contestCache,
        IEventSerializer eventSerializer,
        IClock clock,
        IServiceProvider serviceProvider,
        MachineConfig machineConfig,
        IMapper mapper)
    {
        _logger = logger;
        _asymmetricAlgorithmAdapter = asymmetricAlgorithmAdapter;
        _pkcs11DeviceAdapter = pkcs11DeviceAdapter;
        _eventSerializer = eventSerializer;
        _clock = clock;
        _serviceProvider = serviceProvider;
        _machineConfig = machineConfig;
        _mapper = mapper;
        _contestCache = contestCache;
    }

    internal async Task FillBusinessMetadata<TAggregate>(TAggregate aggregate)
        where TAggregate : BaseEventSourcingAggregate
    {
        foreach (var ev in aggregate.GetUncommittedEvents())
        {
            // non directly contest related events such as counting circle, domain of influence and canton settings events have no business metadata.
            if (ev.Metadata is ProtoEventSignatureBusinessMetadata businessMetadata)
            {
                await FillBusinessMetadata(businessMetadata, aggregate.StreamName, ev.Data, ev.Id);
            }
        }
    }

    internal void UpdateSignedEventCount(IReadOnlyCollection<IDomainEvent> publishedEvents)
    {
        foreach (var ev in publishedEvents)
        {
            // non directly contest related events such as counting circle, domain of influence and canton settings events have no business metadata.
            if (ev.Metadata is not ProtoEventSignatureBusinessMetadata businessMetadata)
            {
                continue;
            }

            var contestId = GuidParser.Parse(businessMetadata.ContestId);
            var keyData = _contestCache.Get(contestId)?.KeyData;

            if (keyData == null)
            {
                throw new InvalidOperationException("No key data found to increment the signed event count for a event with business event metadata");
            }

            keyData.IncrementSignedEventCount();
        }
    }

    /// <summary>
    /// Stops event signature for the contest. Deletes the signed public key of the current host and contest.
    /// </summary>
    /// <param name="contestId">Contest id.</param>
    /// <param name="keyData">The contest key data.</param>
    /// <returns>A Task.</returns>
    /// <exception cref="ArgumentException">If no key is assigned.</exception>
    internal async Task StopSignature(Guid contestId, ContestCacheEntryKeyData keyData)
    {
        try
        {
            var authTagPayload = new PublicKeySignatureDeleteAuthenticationTagPayload(
                EventSignatureVersions.V1,
                contestId,
                _machineConfig.Name,
                keyData.Key.Id,
                _clock.UtcNow,
                keyData.SignedEventCount);

            var hsmPayload = new PublicKeySignatureDeleteHsmPayload(
                authTagPayload,
                _asymmetricAlgorithmAdapter.CreateSignature(authTagPayload.ConvertToBytesToSign(), keyData.Key));

            var publicKeyDelete = BuildPublicKeyDelete(hsmPayload);
            using var scope = _serviceProvider.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<EventSignatureWriter>();

            await writer.DeletePublicKey(publicKeyDelete);
            _logger.LogInformation(SecurityLogging.SecurityEventId, "Removed signature key {KeyId} for contest {ContestId}", keyData.Key.Id, contestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(SecurityLogging.SecurityEventId, ex, "Delete public key for contest {ContestId} and key {KeyId} failed", contestId, keyData.Key.Id);
        }
    }

    internal async Task EnsureActiveSignature(Guid contestId, DateTime eventTimestamp)
    {
        var contest = _contestCache.Get(contestId);

        if (contest == null)
        {
            contest = new ContestCacheEntry
            {
                Id = contestId,
            };
            _contestCache.Add(contest);
        }

        if (contest.KeyData == null)
        {
            contest.KeyData = await StartSignature(contestId, eventTimestamp);
            return;
        }

        // if the key expired but the scheduler did not stop the signature yet, it should be explicitly stopped and restarted here.
        if (contest.KeyData.ValidTo < eventTimestamp)
        {
            await StopSignature(contestId, contest.KeyData);
            contest.KeyData.Key.Dispose();
            contest.KeyData = await StartSignature(contestId, eventTimestamp);
        }
    }

    private async Task<ContestCacheEntryKeyData> StartSignature(Guid contestId, DateTime timestamp)
    {
        EcdsaPrivateKey? key = null;

        try
        {
            key = _asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

            var authTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
                EventSignatureVersions.V1,
                contestId,
                _machineConfig.Name,
                key.Id,
                key.PublicKey,
                timestamp,
                timestamp.NextUtcDate(true));

            var hsmPayload = new PublicKeySignatureCreateHsmPayload(
                authTagPayload,
                _asymmetricAlgorithmAdapter.CreateSignature(authTagPayload.ConvertToBytesToSign(), key));

            var publicKeyCreate = BuildPublicKeyCreate(hsmPayload);

            using var scope = _serviceProvider.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<EventSignatureWriter>();
            await writer.CreatePublicKey(publicKeyCreate);
            var keyData = new ContestCacheEntryKeyData(key, hsmPayload.ValidFrom, hsmPayload.ValidTo);

            _logger.LogInformation(
                SecurityLogging.SecurityEventId,
                "Created signature key pair {KeyId} for contest {ContestId} ({ValidFrom} - {ValidTo})",
                key.Id,
                contestId,
                keyData.ValidFrom,
                keyData.ValidTo);

            return keyData;
        }
        catch (Exception ex)
        {
            key?.Dispose();
            _logger.LogError(ex, "Start signature for contest {ContestId} failed", contestId);
            throw;
        }
    }

    private EventSignatureBusinessMetadata BuildBusinessMetadata(string streamName, IMessage eventData, Guid contestId, Guid eventId)
    {
        var keyData = _contestCache.Get(contestId)?.KeyData;
        if (keyData == null)
        {
            throw new InvalidOperationException($"No key data found to build event signature business metadata for contest {contestId}");
        }

        return CreateBusinessMetadata(
            eventId,
            eventData,
            keyData,
            streamName,
            contestId);
    }

    private EventSignaturePublicKeyCreate BuildPublicKeyCreate(PublicKeySignatureCreateHsmPayload hsmPayload)
    {
        var hsmSignature = _pkcs11DeviceAdapter.CreateSignature(hsmPayload.ConvertToBytesToSign());
        return new EventSignaturePublicKeyCreate
        {
            SignatureVersion = hsmPayload.SignatureVersion,
            ContestId = hsmPayload.ContestId,
            HostId = hsmPayload.HostId,
            KeyId = hsmPayload.KeyId,
            PublicKey = hsmPayload.PublicKey,
            ValidFrom = hsmPayload.ValidFrom,
            ValidTo = hsmPayload.ValidTo,
            AuthenticationTag = hsmPayload.AuthenticationTag,
            HsmSignature = hsmSignature,
        };
    }

    private EventSignaturePublicKeyDelete BuildPublicKeyDelete(PublicKeySignatureDeleteHsmPayload hsmPayload)
    {
        var hsmSignature = _pkcs11DeviceAdapter.CreateSignature(hsmPayload.ConvertToBytesToSign());
        return new EventSignaturePublicKeyDelete
        {
            SignatureVersion = hsmPayload.SignatureVersion,
            ContestId = hsmPayload.ContestId,
            HostId = hsmPayload.HostId,
            KeyId = hsmPayload.KeyId,
            DeletedAt = hsmPayload.DeletedAt,
            SignedEventCount = hsmPayload.SignedEventCount,
            AuthenticationTag = hsmPayload.AuthenticationTag,
            HsmSignature = hsmSignature,
        };
    }

    private EventSignatureBusinessMetadata CreateBusinessMetadata(
        Guid eventId,
        IMessage eventData,
        ContestCacheEntryKeyData keyData,
        string streamName,
        Guid contestId)
    {
        if (keyData.Key.PrivateKey == null || keyData.Key.PrivateKey.Length == 0)
        {
            throw new ArgumentException($"Cannot create event metadata for contest {contestId} without a private key");
        }

        var timestamp = EventInfoUtils.GetEventInfo(eventData).Timestamp.ToDateTime();
        if (timestamp < keyData.ValidFrom || timestamp > keyData.ValidTo)
        {
            throw new ArgumentException($"Cannot create event metadata because the current key {keyData.Key.Id} for contest {contestId} is not valid anymore ({keyData.ValidFrom} - {keyData.ValidTo}).");
        }

        var businessPayload = new EventSignatureBusinessPayload(
            EventSignatureVersions.V1,
            eventId,
            streamName,
            _eventSerializer.Serialize(eventData).ToArray(),
            contestId,
            _machineConfig.Name,
            keyData.Key.Id,
            timestamp);

        var businessSignature = CreateBusinessSignature(businessPayload, keyData.Key);

        return new EventSignatureBusinessMetadata(
            contestId,
            EventSignatureVersions.V1,
            businessPayload.HostId,
            keyData.Key.Id,
            businessSignature);
    }

    private async Task FillBusinessMetadata(ProtoEventSignatureBusinessMetadata businessMetadata, string streamName, IMessage eventData, Guid eventId)
    {
        // If metadata is defined, it will only contain a contest id.
        var contestId = GuidParser.Parse(businessMetadata.ContestId);
        var eventTimestamp = EventInfoUtils.GetEventInfo(eventData).Timestamp.ToDateTime();

        await EnsureActiveSignature(contestId, eventTimestamp);

        var domainBusinessMetadata = BuildBusinessMetadata(streamName, eventData, contestId, eventId);
        _mapper.Map(domainBusinessMetadata, businessMetadata);
    }

    private byte[] CreateBusinessSignature(EventSignatureBusinessPayload businessPayload, EcdsaPrivateKey key)
    {
        return _asymmetricAlgorithmAdapter.CreateSignature(businessPayload.ConvertToBytesToSign(), key);
    }
}
