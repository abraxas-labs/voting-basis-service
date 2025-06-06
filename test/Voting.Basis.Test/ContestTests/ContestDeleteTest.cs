// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.EventSignature;
using Voting.Basis.Data.Models;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestDeleteTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";
    private string? _authTestContestId;

    public ContestDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped, false);
    }

    [Fact]
    public async Task InvalidGuidShouldThrow()
        => await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task TestNotFound()
        => await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task TestWithPoliticalBusinessShouldDeleteAll()
    {
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await MajorityElectionUnionMockedData.Seed(RunScoped);

        var client = CreateAuthorizedClient(DomainOfInfluenceMockedData.Bund.SecureConnectId, Roles.CantonAdmin);
        await client.DeleteAsync(new DeleteContestRequest
        {
            Id = ContestMockedData.IdBundContest,
        });

        var publishedEvents = EventPublisherMock.AllPublishedEvents.ToArray();
        var eventNamesInOrder = publishedEvents.Select(x => x.Data.GetType().Name);
        eventNamesInOrder.MatchSnapshot();

        // Check to see if event processing works correctly with the events in order
        await TestEventPublisher.Publish(publishedEvents.Select(x => x.Data).ToArray());
    }

    [Fact]
    public async Task Test()
    {
        var id = ContestMockedData.IdThurgauNoPoliticalBusinesses;
        await CantonAdminClient.DeleteAsync(new DeleteContestRequest
        {
            Id = id,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestDeleted, EventSignatureBusinessMetadata>();

        eventData.ContestId.Should().Be(ContestMockedData.IdThurgauNoPoliticalBusinesses);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(id);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        var id = ContestMockedData.IdThurgauNoPoliticalBusinesses;
        await ShouldTriggerEventSignatureAndSignEvent(id, async () =>
        {
            await CantonAdminClient.DeleteAsync(new() { Id = id });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ContestMockedData.IdGossau;
        await TestEventPublisher.Publish(new ContestDeleted { ContestId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.Contests.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedEventProcessedMessage(ContestDeleted.Descriptor, idGuid);
    }

    [Fact]
    public async Task TestForeignDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = ContestMockedData.IdUzwilEvoting,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestParentContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = ContestMockedData.IdBundContest,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ContestWhichIsSetAsPreviousContestShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == ContestMockedData.BundContest.Id,
            c => c.PreviousContestId = ContestMockedData.PastLockedContestNoPoliticalBusinesses.Id);

        await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses,
            }),
            StatusCode.FailedPrecondition,
            "previous contest");
    }

    [Fact]
    public async Task TestTransientCatchUpInReplay()
    {
        var testEventPublisher = GetService<TestEventPublisher>();
        var contestCache = GetService<ContestCache>();

        var contestId = Guid.Parse(ContestMockedData.IdGossau);

        contestCache.Get(contestId).Should().NotBeNull();

        await testEventPublisher.Publish(
            true,
            new ContestDeleted
            {
                EventInfo = GetMockedEventInfo(),
                ContestId = contestId.ToString(),
            });

        contestCache.Get(contestId).Should().BeNull();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessingWithExistingKey()
    {
        var testEventPublisher = GetService<TestEventPublisher>();

        // Make sure that no existing/expired keys are present, which interfere with this test
        var contestCache = GetService<ContestCache>();
        contestCache.Clear();

        // Make sure that an active key exists. This also ensures that the correct aggregate exists
        var contestId = Guid.Parse(ContestMockedData.IdGossau);
        var signatureService = GetService<EventSignatureService>();
        await signatureService.EnsureActiveSignature(contestId, MockedClock.UtcNowDate);

        // Overwrite the key with an expired key, since that should also be removed when a contest is deleted
        // We cannot do this directly with EnsureActiveSignature(), since it would emit the stop event by itself
        var asymmetricAlgorithmAdapter = GetService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
        var key = asymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        contestCache.Get(contestId)!.KeyData = new ContestCacheEntryKeyData(key, DateTime.MinValue, DateTime.MinValue);

        await testEventPublisher.Publish(
            false,
            new ContestDeleted
            {
                EventInfo = GetMockedEventInfo(),
                ContestId = contestId.ToString(),
            });

        contestCache.Get(contestId).Should().BeNull();

        var ev = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeyDeleted, EventSignaturePublicKeyMetadata>();
        ev.Data.KeyId.Should().Be(key.Id);
        ev.Data.AuthenticationTag.Should().NotBeEmpty();
        ev.Metadata!.HsmSignature.Should().NotBeEmpty();

        ev.Data.KeyId = string.Empty;
        ev.Data.AuthenticationTag = ByteString.Empty;
        ev.Metadata.HsmSignature = ByteString.Empty;
        ev.MatchSnapshot();
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessingWithoutKey()
    {
        var testEventPublisher = GetService<TestEventPublisher>();
        var contestCache = GetService<ContestCache>();

        var contestId = Guid.Parse(ContestMockedData.IdGossau);
        contestCache.Get(contestId)!.KeyData = null;

        await testEventPublisher.Publish(
            false,
            new ContestDeleted
            {
                EventInfo = GetMockedEventInfo(),
                ContestId = contestId.ToString(),
            });

        contestCache.Get(contestId).Should().BeNull();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyCreated>().Should().BeEmpty();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Should().BeEmpty();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestContestId == null)
        {
            var response = await ElectionAdminClient.CreateAsync(new CreateContestRequest
            {
                Date = new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            });

            _authTestContestId = response.Id;
        }

        await new ContestService.ContestServiceClient(channel)
            .DeleteAsync(new DeleteContestRequest { Id = _authTestContestId });
        _authTestContestId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
    }
}
