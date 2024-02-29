// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Permission;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Exceptions;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Basis.Core.Services.Write;

public class EventSignatureWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly ILogger<EventSignatureWriter> _logger;
    private readonly EventSignatureConfig _config;

    public EventSignatureWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        ILogger<EventSignatureWriter> logger,
        AppConfig config)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _logger = logger;
        _config = config.Publisher.EventSignature;
    }

    internal Task CreatePublicKey(EventSignaturePublicKeyCreate data)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        return RetryOnVersionMismatchException(async () =>
        {
            var aggregate = await _aggregateRepository.TryGetById<ContestEventSignatureAggregate>(data.ContestId)
                ?? _aggregateFactory.New<ContestEventSignatureAggregate>();

            aggregate.CreatePublicKey(data);
            await _aggregateRepository.Save(aggregate);
        });
    }

    internal Task DeletePublicKey(EventSignaturePublicKeyDelete data)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        return RetryOnVersionMismatchException(async () =>
        {
            var aggregate = await _aggregateRepository.GetById<ContestEventSignatureAggregate>(data.ContestId);
            aggregate.DeletePublicKey(data);
            await _aggregateRepository.Save(aggregate);
        });
    }

    private async Task RetryOnVersionMismatchException(Func<Task> action)
    {
        for (var i = 1; i <= _config.EventWritesMaxAttempts; i++)
        {
            try
            {
                await action();
                return;
            }
            catch (VersionMismatchException e) when (i < _config.EventWritesMaxAttempts)
            {
                _logger.LogInformation(e, "Version mismatch when trying to write event signature event, attempt {AttemptNr} of {TotalAttempts}", i, _config.EventWritesMaxAttempts);
                await Task.Delay(Random.Shared.Next(_config.EventWritesRetryMinDelayMillis, _config.EventWritesRetryMaxDelayMillis));
            }
        }
    }
}
