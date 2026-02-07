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

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.UpdateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.ProportionalElection.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(NewValidEvent());

        var proportionalElection = await CantonAdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id.ToString(),
        });
        proportionalElection.MatchSnapshot("event");

        await AssertHasPublishedEventProcessedMessage(ProportionalElectionUpdated.Descriptor, id);
    }

    [Theory]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum)]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)]
    public async Task TestProcessorWithDeprecatedMandateAlgorithms(SharedProto.ProportionalElectionMandateAlgorithm deprecatedMandateAlgorithm, SharedProto.ProportionalElectionMandateAlgorithm expectedMandateAlgorithm)
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund);

        await TestEventPublisher.Publish(
            new ProportionalElectionUpdated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id.ToString(),
                    PoliticalBusinessNumber = "6000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Official Description") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated Short Description") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = deprecatedMandateAlgorithm,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                    ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                    EnforceReviewProcedureForCountingCircles = true,
                    CandidateCheckDigit = false,
                    EnforceCandidateCheckDigitForCountingCircles = true,
                },
            });

        var proportionalElection = await CantonAdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id.ToString(),
        });
        proportionalElection.MandateAlgorithm.Should().Be(expectedMandateAlgorithm);
    }

    [Fact]
    public async Task TestAggregateChangeNumberOfMandatesShouldUpdateListCandidatesOk()
    {
        var electionId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);
        var idOk = Guid.Parse("7c2f9e28-cf35-4b80-9f7d-13a4c324203e");
        var idNotOk = Guid.Parse("b41ed7e9-b97b-493c-9be2-3cd573ce6a65");
        await RunOnDb(db =>
        {
            db.ProportionalElectionLists.AddRange(
                new ProportionalElectionList
                {
                    Id = idOk,
                    ProportionalElectionId = electionId,
                    BlankRowCount = 1,
                    CountOfCandidates = 1,
                    CandidateCountOk = false,
                },
                new ProportionalElectionList
                {
                    Id = idNotOk,
                    ProportionalElectionId = electionId,
                    BlankRowCount = 1,
                    CountOfCandidates = 3,
                    CandidateCountOk = true,
                });

            return db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(NewValidEvent());

        var okList = await RunOnDb(async db => await db.ProportionalElectionLists.FindAsync(idOk));
        okList!.CandidateCountOk.Should().BeTrue();

        var notOkList = await RunOnDb(async db => await db.ProportionalElectionLists.FindAsync(idNotOk));
        notOkList!.CandidateCountOk.Should().BeFalse();
    }

    [Fact]
    public async Task ContestChangeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.ContestId = ContestMockedData.IdBundContest;
            })),
            StatusCode.InvalidArgument,
            "ContestId");
    }

    [Fact]
    public Task UpdateNumberOfMandatesOnActiveElectionShouldThrow()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(v => v.NumberOfMandates = 10)),
            StatusCode.FailedPrecondition,
            nameof(ModificationNotAllowedException));
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
        await RunOnDb(async db =>
        {
            await db.ProportionalElectionUnions.Where(u => u.ProportionalElectionUnionEntries
                .Any(e => e.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen)))
                .ExecuteDeleteAsync();
        });

        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum;
            })),
            StatusCode.FailedPrecondition,
            nameof(ModificationNotAllowedException));
    }

    [Fact]
    public async Task GreaterSampleSizeThanBallotSizeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o => o.BallotBundleSampleSize = 9999)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task UpdateMandateAlgorithmWithPbUnionsShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.Id == DomainOfInfluenceMockedData.GuidStGallen,
            doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms = new()
            {
                ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum,
            });

        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o => o.MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum)),
            StatusCode.FailedPrecondition,
            "The mandate algorithm may only be changed in the case of proportional elections without unions");
    }

    [Fact]
    public async Task ProportionalElectionUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        var id = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund);

        await CantonAdminClient.UpdateAsync(new UpdateProportionalElectionRequest
        {
            Id = id.ToString(),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            ContestId = ContestMockedData.IdBundContest,
            PoliticalBusinessNumber = "6000new",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Update Proporzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl") },
            Active = true,
            AutomaticBallotBundleNumberGeneration = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.AutomaticBallotBundleNumberGeneration,
            BallotBundleSize = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.BallotBundleSize,
            BallotBundleSampleSize = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.BallotBundleSampleSize,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            NumberOfMandates = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.NumberOfMandates,
            AutomaticEmptyVoteCounting = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.AutomaticEmptyVoteCounting,
            CandidateCheckDigit = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.CandidateCheckDigit,
            MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        });

        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await CantonAdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id.ToString(),
        });
        election.MatchSnapshot("reponse");

        await AssertHasPublishedEventProcessedMessage(ProportionalElectionUpdated.Descriptor, id);
    }

    [Fact]
    public async Task ProportionalElectionUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
                o.ContestId = ContestMockedData.IdBundContest;
                o.DomainOfInfluenceId = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.DomainOfInfluenceId.ToString();
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task ProportionalElectionInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
                o.ContestId = ContestMockedData.IdBundContest;
                o.DomainOfInfluenceId = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.DomainOfInfluenceId.ToString();
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
                x.Id = ProportionalElectionMockedData.IdGossauProportionalElectionEVotingApprovedInContestStGallen;
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
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public Task DuplicatePoliticalBusinessIdShouldThrow()
    {
        return AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(v => v.PoliticalBusinessNumber = "500")),
            StatusCode.AlreadyExists);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    private UpdateProportionalElectionRequest NewValidRequest(
        Action<UpdateProportionalElectionRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionRequest
        {
            Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            PoliticalBusinessNumber = "6541",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Update Proporzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            ContestId = ContestMockedData.IdStGallenEvoting,
            Active = false,
            AutomaticBallotBundleNumberGeneration = true,
            AutomaticEmptyVoteCounting = false,
            BallotBundleSize = 25,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = false,
            EnforceEmptyVoteCountingForCountingCircles = true,
            MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            FederalIdentification = 29348929,
        };

        customizer?.Invoke(request);
        return request;
    }

    private ProportionalElectionUpdated NewValidEvent(
        Action<ProportionalElectionUpdated>? customizer = null)
    {
        var ev = new ProportionalElectionUpdated
        {
            ProportionalElection = new ProportionalElectionEventData
            {
                Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                PoliticalBusinessNumber = "6000",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Update Proporzwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContestId = ContestMockedData.IdStGallenEvoting,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSize = 25,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                CandidateCheckDigit = false,
                EnforceEmptyVoteCountingForCountingCircles = true,
                MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                NumberOfMandates = 2,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                EnforceCandidateCheckDigitForCountingCircles = true,
                FederalIdentification = 29348929,
            },
        };

        customizer?.Invoke(ev);
        return ev;
    }
}
