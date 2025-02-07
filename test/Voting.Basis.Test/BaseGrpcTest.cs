// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Basis.Test;

public abstract class BaseGrpcTest<TService> : GrpcAuthorizationBaseTest<TestApplicationFactory, TestStartup>
    where TService : ClientBase<TService>
{
    private readonly Lazy<TService> _adminClient;
    private readonly Lazy<TService> _electionAdminClient;
    private readonly Lazy<TService> _electionAdminReadOnlyClient;
    private readonly Lazy<TService> _electionSupporterClient;
    private readonly Lazy<TService> _cantonAdminClient;
    private readonly Lazy<TService> _cantonAdminReadOnlyClient;
    private readonly Lazy<TService> _electionAdminUzwilClient;
    private readonly Lazy<TService> _apiReaderClient;
    private readonly Lazy<TService> _apiReaderDoiClient;
    private readonly Lazy<TService> _zurichCantonAdminClient;
    private int _currentEventNumber;

    protected BaseGrpcTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetDb();

        TestEventPublisher = GetService<TestEventPublisherAdapter>();
        EventPublisherMock = GetService<EventPublisherMock>();
        AggregateRepositoryMock = GetService<AggregateRepositoryMock>();
        EventPublisherMock.Clear();
        AggregateRepositoryMock.Clear();

        _adminClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.Admin));
        _cantonAdminClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.CantonAdmin));
        _cantonAdminReadOnlyClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.CantonAdminReadOnly));
        _electionAdminClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.ElectionAdmin));
        _electionAdminReadOnlyClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.ElectionAdminReadOnly));
        _electionAdminUzwilClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantUzwil.Id, Roles.ElectionAdmin));
        _electionSupporterClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.ElectionSupporter));
        _apiReaderClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.ApiReader));
        _apiReaderDoiClient = new(() => CreateAuthorizedClient(SecureConnectTestDefaults.MockedTenantDefault.Id, Roles.ApiReaderDoi));
        _zurichCantonAdminClient = new(() => CreateAuthorizedClient("zürich-sec-id", Roles.CantonAdmin));

        MessagingTestHarness = GetService<InMemoryTestHarness>();
    }

    protected EventPublisherMock EventPublisherMock { get; }

    protected AggregateRepositoryMock AggregateRepositoryMock { get; }

    protected TestEventPublisherAdapter TestEventPublisher { get; }

    protected TService AdminClient => _adminClient.Value;

    protected TService CantonAdminClient => _cantonAdminClient.Value;

    protected TService CantonAdminReadOnlyClient => _cantonAdminReadOnlyClient.Value;

    protected TService ElectionAdminClient => _electionAdminClient.Value;

    protected TService ElectionAdminReadOnlyClient => _electionAdminReadOnlyClient.Value;

    protected TService ElectionSupporterClient => _electionSupporterClient.Value;

    protected TService ElectionAdminUzwilClient => _electionAdminUzwilClient.Value;

    protected TService ApiReaderClient => _apiReaderClient.Value;

    protected TService ApiReaderDoiClient => _apiReaderDoiClient.Value;

    protected TService ZurichCantonAdminClient => _zurichCantonAdminClient.Value;

    protected InMemoryTestHarness MessagingTestHarness { get; set; }

    /// <summary>
    /// Authorized roles should have access to the method.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task AuthorizedRolesShouldHaveAccess()
    {
        foreach (var role in AuthorizedRoles())
        {
            try
            {
                await AuthorizationTestCall(CreateGrpcChannel(role == NoRole ? Array.Empty<string>() : new[] { role }));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Call failed for role {role}: {ex}");
            }
        }
    }

    protected abstract IEnumerable<string> AuthorizedRoles();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        return Roles
            .All()
            .Append(NoRole)
            .Except(AuthorizedRoles());
    }

    protected async Task AssertHasPublishedMessage<T>(Func<T, bool> predicate, bool hasMessage = true)
        where T : class
    {
        var hasOne = await MessagingTestHarness.Published.Any<T>(x => predicate(x.Context.Message));
        hasOne.Should().Be(hasMessage);
    }

    protected TService CreateAuthorizedClient(string tenantId, params string[] roles)
    {
        return (TService)Activator.CreateInstance(typeof(TService), CreateGrpcChannel(true, tenantId, roles: roles))!;
    }

    protected Task PublishMessage<T>(T msg) => GetService<IBus>().Publish(msg);

    protected Task RunOnDb(Func<DataContext, Task> action)
        => RunScoped(action);

    protected Task<TResult> RunOnDb<TResult>(Func<DataContext, Task<TResult>> action)
        => RunScoped(action);

    protected async Task RunEvents<TEvent>(bool clear = true)
        where TEvent : IMessage<TEvent>
    {
        var events = EventPublisherMock.GetPublishedEvents<TEvent>().ToArray();
        await TestEventPublisher.Publish(
            _currentEventNumber,
            events);
        _currentEventNumber += events.Length;
        if (clear)
        {
            EventPublisherMock.Clear();
        }
    }

    protected void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        DatabaseUtil.Truncate(db);
    }

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

    protected Task SetContestState(string contestId, ContestState state)
        => ModifyDbEntities<Contest>(c => c.Id == Guid.Parse(contestId), c => c.State = state);

    protected Task ModifyDbEntities<TEntity>(Expression<Func<TEntity, bool>> predicate, Action<TEntity> modifier)
        where TEntity : class
    {
        return RunOnDb(async db =>
        {
            var set = db.Set<TEntity>();
            var entities = await set.AsTracking().Where(predicate).ToListAsync();

            foreach (var entity in entities)
            {
                modifier(entity);
            }

            await db.SaveChangesAsync();
        });
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
