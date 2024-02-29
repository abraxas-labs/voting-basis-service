// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ContestState = Voting.Basis.Data.Models.ContestState;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionDeleteTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";

    public MajorityElectionDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteMajorityElectionRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteAsync(new DeleteMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionDeleted, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionId.Should().Be(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteAsync(new DeleteMajorityElectionRequest
            {
                Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen;
        await TestEventPublisher.Publish(new MajorityElectionDeleted { MajorityElectionId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.MajorityElections.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(Guid.Parse(id), EntityState.Deleted));
    }

    [Fact]
    public async Task WithSecondaryElectionShouldThrow()
    {
        await AdminClient.CreateSecondaryMajorityElectionAsync(new()
        {
            PoliticalBusinessNumber = "10246",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            Active = true,
            NumberOfMandates = 5,
            AllowedCandidates = SecondaryMajorityElectionAllowedCandidates.MayExistInPrimaryElection,
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });

        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new()
            {
                Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Majority election with existing secondary elections cannot be deleted");
    }

    [Fact]
    public async Task MajorityElectionOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteMajorityElectionRequest
            {
                Id = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestParentMajorityElectionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteMajorityElectionRequest
            {
                Id = MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteMajorityElectionRequest
            {
                Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var id = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;

        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .DeleteAsync(new DeleteMajorityElectionRequest { Id = id });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
