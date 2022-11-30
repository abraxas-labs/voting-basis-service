// (c) Copyright 2022 by Abraxas Informatik AG
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
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestDeleteTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

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
            async () => await AdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task TestNotFound()
        => await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task TestExistingPoliticalBusinessShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = ContestMockedData.IdGossau,
            }),
            StatusCode.FailedPrecondition,
            "existing political businesses");
    }

    [Fact]
    public async Task Test()
    {
        var id = ContestMockedData.IdThurgauNoPoliticalBusinesses;
        await AdminClient.DeleteAsync(new DeleteContestRequest
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
            await AdminClient.DeleteAsync(new() { Id = id });
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

        await AssertHasPublishedMessage<ContestOverviewChangeMessage>(
            x => x.Contest.HasEqualIdAndNewEntityState(idGuid, EntityState.Deleted));
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
            async () => await AdminClient.DeleteAsync(new DeleteContestRequest
            {
                Id = ContestMockedData.IdPastLockedContestNoPoliticalBusinesses,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ContestWhichIsSetAsPreviousContestShouldThrow()
    {
        await ModifyDbEntities<Data.Models.Contest>(
            c => c.Id == ContestMockedData.BundContest.Id,
            c => c.PreviousContestId = ContestMockedData.PastLockedContestNoPoliticalBusinesses.Id);

        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteContestRequest
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
        var contestCache = GetService<ContestCache>();
        var asymmetricAlgorithmAdapter = GetService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();

        var key = asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

        var contestId = Guid.Parse(ContestMockedData.IdGossau);

        // should emit the key deleted event even if the key is expired.
        contestCache.Get(contestId)!.KeyData = new ContestCacheEntryKeyData(key, DateTime.MinValue, DateTime.MinValue);

        await testEventPublisher.Publish(
            false,
            new ContestDeleted
            {
                EventInfo = GetMockedEventInfo(),
                ContestId = contestId.ToString(),
            });

        contestCache.Get(contestId).Should().BeNull();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyCreated>().Should().BeEmpty();

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
        var id = ContestMockedData.IdGossau;

        await new ContestService.ContestServiceClient(channel)
            .DeleteAsync(new DeleteContestRequest { Id = id });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
