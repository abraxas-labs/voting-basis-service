// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await ElectionAdminClient.UpdateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.MajorityElection.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ElectionAdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var electionId = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen);
        var ballotGroupOkNowNotAfterUpdate = Guid.Parse("aebd862f-0eb3-47d4-83f2-e53440083c08");
        var ballotGroupNotOkNowOkAfterUpdate = Guid.Parse("3320bf4e-f6f2-4c40-9c37-247a37f8aad6");
        await RunOnDb(db =>
        {
            db.MajorityElectionBallotGroups.AddRange(
                new MajorityElectionBallotGroup
                {
                    Id = ballotGroupOkNowNotAfterUpdate,
                    Description = "okNowNotAfterUpdate",
                    Position = 1,
                    ShortDescription = "okNowNotAfterUpdate",
                    MajorityElectionId = electionId,
                    Entries = new List<MajorityElectionBallotGroupEntry>
                    {
                            new MajorityElectionBallotGroupEntry
                            {
                                BlankRowCount = 2,
                                CountOfCandidates = 2,
                                IndividualCandidatesVoteCount = 3,
                                PrimaryMajorityElectionId = electionId,
                            },
                    },
                },
                new MajorityElectionBallotGroup
                {
                    Id = ballotGroupNotOkNowOkAfterUpdate,
                    Description = "notOkNowOkAfterUpdate",
                    Position = 2,
                    ShortDescription = "notOkNowOkAfterUpdate",
                    MajorityElectionId = electionId,
                    Entries = new List<MajorityElectionBallotGroupEntry>
                    {
                            new MajorityElectionBallotGroupEntry
                            {
                                BlankRowCount = 1,
                                CountOfCandidates = 1,
                                IndividualCandidatesVoteCount = 0,
                                PrimaryMajorityElectionId = electionId,
                            },
                    },
                });

            return db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(NewValidEvent());

        var majorityElection = await ElectionAdminClient.GetAsync(new GetMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });
        majorityElection.MatchSnapshot("data");

        var ballotGroup1 =
            await RunOnDb(db => db.MajorityElectionBallotGroups.Include(x => x.Entries).SingleAsync(x => x.Id == ballotGroupOkNowNotAfterUpdate));
        ballotGroup1.Entries.Single().CandidateCountOk.Should().BeFalse();

        var ballotGroup2 =
            await RunOnDb(db => db.MajorityElectionBallotGroups.Include(x => x.Entries).SingleAsync(x => x.Id == ballotGroupNotOkNowOkAfterUpdate));
        ballotGroup2.Entries.Single().CandidateCountOk.Should().BeTrue();

        await AssertHasPublishedEventProcessedMessage(MajorityElectionUpdated.Descriptor, Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen));
    }

    [Fact]
    public async Task ContestChangeShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.ContestId = ContestMockedData.IdBundContest)),
            StatusCode.InvalidArgument,
            "ContestId");
    }

    [Fact]
    public async Task DomainOfInfluenceChangeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
            })),
            StatusCode.FailedPrecondition,
            nameof(ModificationNotAllowedException));
    }

    [Fact]
    public async Task MandateAlgorithmChangeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.AbsoluteMajority;
            })),
            StatusCode.FailedPrecondition,
            nameof(ModificationNotAllowedException));
    }

    [Fact]
    public async Task GreaterSampleSizeThanBallotSizeShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.BallotBundleSampleSize = 9999)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task InvalidReportDomainOfInfluenceLevelShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.ReportDomainOfInfluenceLevel = 13)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        var id = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestBund);

        await ElectionAdminClient.UpdateAsync(new UpdateMajorityElectionRequest
        {
            Id = id.ToString(),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            ContestId = ContestMockedData.IdBundContest,
            PoliticalBusinessNumber = "5478new",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Majorzwahl Update") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Majorzwahl Update") },
            Active = true,
            AutomaticBallotBundleNumberGeneration = MajorityElectionMockedData.GossauMajorityElectionInContestBund.AutomaticBallotBundleNumberGeneration,
            BallotBundleSize = MajorityElectionMockedData.GossauMajorityElectionInContestBund.BallotBundleSize,
            BallotBundleSampleSize = MajorityElectionMockedData.GossauMajorityElectionInContestBund.BallotBundleSampleSize,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            NumberOfMandates = MajorityElectionMockedData.GossauMajorityElectionInContestBund.NumberOfMandates,
            ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        });

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await ElectionAdminClient.GetAsync(new GetMajorityElectionRequest
        {
            Id = id.ToString(),
        });
        election.MatchSnapshot("reponse");

        await AssertHasPublishedEventProcessedMessage(MajorityElectionAfterTestingPhaseUpdated.Descriptor, id);
    }

    [Fact]
    public async Task MajorityElectionUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
                o.ContestId = ContestMockedData.IdBundContest;
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task MajorityElectionInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
                o.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
                o.ContestId = ContestMockedData.IdPastLockedContest;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(x =>
            {
                x.Id = MajorityElectionMockedData.IdGossauMajorityElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    [Fact]
    public async Task VirtualTopLevelDomainOfInfluenceShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.Id == DomainOfInfluenceMockedData.GuidStGallen,
            x => x.VirtualTopLevel = true);

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public Task DuplicatePoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(v => v.PoliticalBusinessNumber = "500")),
            StatusCode.AlreadyExists);
    }

    [Fact]
    public Task DuplicateSecondaryMajorityElectionPoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(v =>
            {
                v.Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
                v.PoliticalBusinessNumber = "n1";
            })),
            StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task DisableIndividualVotesWithExistingBallotGroupIndividualVotesShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateAsync(NewValidRequest(x =>
            {
                x.Id = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen;
                x.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;
            })),
            StatusCode.InvalidArgument,
            "Cannot disable individual candidates when there are individual candidates vote count defined on ballot group entries");
    }

    [Fact]
    public Task UpdateNumberOfMandatesOnActiveElectionShouldThrow()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(v => v.NumberOfMandates = 1)),
            StatusCode.FailedPrecondition,
            nameof(MajorityElectionActiveNumberOfMandatesChangeException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    private UpdateMajorityElectionRequest NewValidRequest(
        Action<UpdateMajorityElectionRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            PoliticalBusinessNumber = "5478",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Majorzwahl Update") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Majorzwahl Update") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            ContestId = ContestMockedData.IdStGallenEvoting,
            Active = false,
            AutomaticBallotBundleNumberGeneration = true,
            AutomaticEmptyVoteCounting = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 5,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = false,
            EnforceEmptyVoteCountingForCountingCircles = true,
            MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
            ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            NumberOfMandates = 5,
            ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            IndividualCandidatesDisabled = true,
            FederalIdentification = 92834984,
        };

        customizer?.Invoke(request);
        return request;
    }

    private MajorityElectionUpdated NewValidEvent(
        Action<MajorityElectionUpdated>? customizer = null)
    {
        var ev = new MajorityElectionUpdated
        {
            MajorityElection = new MajorityElectionEventData
            {
                Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                PoliticalBusinessNumber = "8164",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContestId = ContestMockedData.IdStGallenEvoting,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSize = 25,
                BallotBundleSampleSize = 8,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                CandidateCheckDigit = false,
                EnforceEmptyVoteCountingForCountingCircles = true,
                MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
                ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
                EnforceResultEntryForCountingCircles = false,
                NumberOfMandates = 2,
                ReportDomainOfInfluenceLevel = 2,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                EnforceCandidateCheckDigitForCountingCircles = true,
                IndividualCandidatesDisabled = true,
                FederalIdentification = 92834984,
            },
        };

        customizer?.Invoke(ev);
        return ev;
    }
}
