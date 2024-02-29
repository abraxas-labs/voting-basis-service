// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Data;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Basis.Test;

public class BaseTest : BaseTest<TestApplicationFactory, TestStartup>
{
    public BaseTest(TestApplicationFactory factory)
        : base(factory)
    {
        TestEventPublisher = GetService<TestEventPublisherAdapter>();
        EventPublisherMock = GetService<EventPublisherMock>();
        AggregateRepositoryMock = GetService<AggregateRepositoryMock>();

        EventPublisherMock.Clear();
        AggregateRepositoryMock.Clear();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        DatabaseUtil.Truncate(db);
    }

    protected TestEventPublisherAdapter TestEventPublisher { get; }

    protected EventPublisherMock EventPublisherMock { get; }

    protected AggregateRepositoryMock AggregateRepositoryMock { get; }

    protected Task RunOnDb(Func<DataContext, Task> action)
        => RunScoped(action);

    protected Task<TResult> RunOnDb<TResult>(Func<DataContext, Task<TResult>> action)
        => RunScoped(action);

    protected Task<TEntity> GetDbEntity<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
        => RunOnDb(db => db.Set<TEntity>().FirstAsync(predicate));

    protected EventInfo GetMockedEventInfo(long additionalSeconds = 0)
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980476 + additionalSeconds,
            },
            Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
            User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
        };
    }

    protected async Task ShouldTriggerEventSignatureAndSignEvent(string contestId, Func<Task<EventWithMetadata>> testAction)
    {
        var contestCache = GetService<ContestCache>();
        var entry = contestCache.Get(Guid.Parse(contestId));

        var keyData = entry?.KeyData;

        // remove the key to test to ensure that the event signature is explicitly started.
        if (entry != null)
        {
            entry.KeyData = null;
        }

        var ev = await testAction();
        EnsureIsSignedBusinessEvent(ev, contestId);

        if (entry != null)
        {
            entry.KeyData = keyData;
        }

        // ensure that a public key signed event got emitted.
        var publicKeySignedEvent = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeyCreated, EventSignaturePublicKeyMetadata>();
        publicKeySignedEvent.Data.ContestId.Should().Be(contestId);
        publicKeySignedEvent.Data.AuthenticationTag.Should().NotBeEmpty();
        publicKeySignedEvent.Metadata!.HsmSignature.Should().NotBeEmpty();
    }

    protected void EnsureIsSignedBusinessEvent(EventWithMetadata ev, string contestId)
    {
        var eventSignatureMetadata = ev.Metadata as EventSignatureBusinessMetadata;
        eventSignatureMetadata.Should().NotBeNull();

        eventSignatureMetadata!.ContestId.Should().Be(contestId);
        eventSignatureMetadata.KeyId.Should().NotBeEmpty();
        eventSignatureMetadata.HostId.Should().NotBeEmpty();
        eventSignatureMetadata.Signature.Should().NotBeEmpty();
    }

    protected async Task ExecuteOnInfiniteValidContestKey(Guid contestId, IServiceProvider sp, Func<Task> testAction)
    {
        var contestCache = sp.GetRequiredService<ContestCache>();
        var asymmetricAlgorithmAdapter = sp.GetRequiredService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();

        if (contestCache.Get(contestId) != null)
        {
            contestCache.Remove(contestId);
        }

        contestCache.Add(new()
        {
            Id = contestId,
            KeyData = new ContestCacheEntryKeyData(asymmetricAlgorithmAdapter.CreateRandomPrivateKey(), DateTime.MinValue, DateTime.MaxValue),
        });

        await testAction.Invoke();
        contestCache.Remove(contestId);
    }
}
