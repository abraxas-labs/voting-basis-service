// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionUpdateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
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
        await AdminClient.UpdateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.ProportionalElection.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(NewValidEvent());

        var proportionalElection = await AdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id.ToString(),
        });
        proportionalElection.MatchSnapshot("event");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id, EntityState.Modified));
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
    public async Task ParentDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(pe =>
            {
                pe.Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau;
                pe.ContestId = ContestMockedData.IdGossau;
                pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ChildDoiWithSameTenantShouldReturnOk()
    {
        await AdminClient.UpdateAsync(NewValidRequest(pe =>
        {
            pe.ContestId = ContestMockedData.IdStGallenEvoting;
            pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUpdated>();
        eventData.MatchSnapshot("event", d => d.ProportionalElection.Id);
    }

    [Fact]
    public async Task SiblingDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(pe =>
            {
                pe.ContestId = ContestMockedData.IdStGallenEvoting;
                pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdThurgau;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ChildDoiWithDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(pe =>
            {
                pe.ContestId = ContestMockedData.IdStGallenEvoting;
                pe.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContestChangeShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.ContestId = ContestMockedData.IdBundContest;
            })),
            StatusCode.InvalidArgument,
            "ContestId");
    }

    [Fact]
    public async Task GreaterSampleSizeThanBallotSizeShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.BallotBundleSampleSize = 9999)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContinuousBallotNumberGenerationWithoutAutomaticGenerationShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.AutomaticBallotBundleNumberGeneration = false;
                o.BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles;
            })),
            StatusCode.InvalidArgument,
            "BallotNumberGeneration");
    }

    [Fact]
    public async Task InvalidMandateAlgorithmByCantonShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        var id = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund);

        await AdminClient.UpdateAsync(new UpdateProportionalElectionRequest
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
        var election = await AdminClient.GetAsync(new GetProportionalElectionRequest
        {
            Id = id.ToString(),
        });
        election.MatchSnapshot("reponse");

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id, EntityState.Modified));
    }

    [Fact]
    public async Task ProportionalElectionUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
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
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
                o.ContestId = ContestMockedData.IdBundContest;
                o.DomainOfInfluenceId = ProportionalElectionMockedData.GossauProportionalElectionInContestBund.DomainOfInfluenceId.ToString();
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
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
            NumberOfMandates = 2,
            ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
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
            },
        };

        customizer?.Invoke(ev);
        return ev;
    }
}
