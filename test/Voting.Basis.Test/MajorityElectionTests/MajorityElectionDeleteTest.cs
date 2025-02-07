// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ContestState = Voting.Basis.Data.Models.ContestState;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestElectionId;

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
            async () => await CantonAdminClient.DeleteAsync(new DeleteMajorityElectionRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.DeleteAsync(new DeleteMajorityElectionRequest
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
            await CantonAdminClient.DeleteAsync(new DeleteMajorityElectionRequest
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
        await CantonAdminClient.CreateSecondaryMajorityElectionAsync(new()
        {
            PoliticalBusinessNumber = "10246",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
            Active = true,
            NumberOfMandates = 5,
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });

        await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new()
            {
                Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Majority election with existing secondary elections cannot be deleted");
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
        if (_authTestElectionId == null)
        {
            var response = await ElectionAdminClient.CreateAsync(new CreateMajorityElectionRequest
            {
                PoliticalBusinessNumber = "9468",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContestId = ContestMockedData.IdStGallenEvoting,
                Active = true,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSize = 13,
                BallotBundleSampleSize = 1,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                CandidateCheckDigit = true,
                EnforceEmptyVoteCountingForCountingCircles = true,
                MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.AbsoluteMajority,
                ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
                EnforceResultEntryForCountingCircles = true,
                NumberOfMandates = 5,
                ReportDomainOfInfluenceLevel = 1,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                EnforceCandidateCheckDigitForCountingCircles = true,
            });
            await RunEvents<MajorityElectionCreated>();

            _authTestElectionId = response.Id;
        }

        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .DeleteAsync(new DeleteMajorityElectionRequest { Id = _authTestElectionId });
        await RunEvents<MajorityElectionDeleted>();
        _authTestElectionId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
