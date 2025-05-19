// (c) Copyright by Abraxas Informatik AG
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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.VoteTests;

public class VoteUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<VoteService.VoteServiceClient>
{
    private readonly TestMapper _mapper;

    public VoteUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
        _mapper = GetService<TestMapper>();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await ElectionAdminClient.UpdateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.Vote.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);

        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.FirstAsync(x => x.Id == VoteMockedData.StGallenVoteInContestStGallen.Id));
        simplePb.BusinessSubType.Should().Be(PoliticalBusinessSubType.Unspecified);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ElectionAdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(NewValidEvent());
        var id = Guid.Parse(VoteMockedData.IdStGallenVoteInContestStGallen);

        var vote = await ElectionAdminClient.GetAsync(new GetVoteRequest
        {
            Id = id.ToString(),
        });
        vote.MatchSnapshot("event");

        await AssertHasPublishedEventProcessedMessage(VoteUpdated.Descriptor, id);
    }

    [Fact]
    public async Task ParentDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(v =>
            {
                v.Id = VoteMockedData.IdGossauVoteInContestGossau;
                v.ContestId = ContestMockedData.IdGossau;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen;
            })),
            StatusCode.InvalidArgument,
            "some ids are not children of the parent node");
    }

    [Fact]
    public async Task ChildDoiWithSameTenantShouldReturnOk()
    {
        await ElectionAdminClient.UpdateAsync(NewValidRequest(v =>
        {
            v.ContestId = ContestMockedData.IdStGallenEvoting;
            v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau;
            v.ReportDomainOfInfluenceLevel = 0;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<VoteUpdated>();
        eventData.MatchSnapshot("event", d => d.Vote.Id);
    }

    [Fact]
    public async Task SiblingDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(v =>
            {
                v.ContestId = ContestMockedData.IdStGallenEvoting;
                v.DomainOfInfluenceId = DomainOfInfluenceMockedData.IdThurgau;
            })),
            StatusCode.InvalidArgument,
            "some ids are not children of the parent node");
    }

    [Fact]
    public async Task ContestChangeShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.ContestId = ContestMockedData.IdBundContest;
                o.InternalDescription = "test";
            })),
            StatusCode.InvalidArgument,
            "ContestId");
    }

    [Fact]
    public async Task ChangeTypeShouldWork()
    {
        await ElectionAdminClient.UpdateAsync(NewValidRequest(x => x.Type = SharedProto.VoteType.VariantQuestionsOnMultipleBallots));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<VoteUpdated>();
        eventData.MatchSnapshot("event", d => d.Vote.Id);

        await RunEvents<VoteUpdated>();
        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.FirstAsync(x => x.Id == VoteMockedData.StGallenVoteInContestStGallen.Id));
        simplePb.BusinessSubType.Should().Be(PoliticalBusinessSubType.VoteVariantBallot);
    }

    [Fact]
    public async Task TestBallotBundleSampleSizePercent100ShouldWork()
    {
        await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.BallotBundleSampleSizePercent = 100));
    }

    [Fact]
    public async Task TestBallotBundleSampleSizePercent0ShouldWork()
    {
        await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.BallotBundleSampleSizePercent = 0));
    }

    [Fact]
    public Task NoBallotsNotEnforcedResultEntryShouldThrow()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.EnforceResultEntryForCountingCircles = false)),
            StatusCode.InvalidArgument,
            "since the detailed result entry is not allowed for this vote, final result entry must be enforced");
    }

    [Fact]
    public async Task SingleVariantsBallotSetNotEnforcedDetailedResultEntryShouldWork()
    {
        await ElectionAdminClient.UpdateBallotAsync(new UpdateBallotRequest
        {
            Id = VoteMockedData.BallotIdStGallenVoteInContestStGallen,
            BallotType = SharedProto.BallotType.VariantsBallot,
            VoteId = VoteMockedData.IdStGallenVoteInContestStGallen,
            BallotQuestions =
                {
                    new ProtoModels.BallotQuestion
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Frage 1") },
                        Type = SharedProto.BallotQuestionType.MainBallot,
                    },
                    new ProtoModels.BallotQuestion
                    {
                        Number = 2,
                        Question = { LanguageUtil.MockAllLanguages("Frage 2") },
                        Type = SharedProto.BallotQuestionType.CounterProposal,
                    },
                },
        });
        await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
        {
            o.EnforceResultEntryForCountingCircles = false;
            o.ResultEntry = SharedProto.VoteResultEntry.Detailed;
        }));
    }

    [Fact]
    public Task NoBallotsDetailedResultEntryShouldThrow()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.ResultEntry = SharedProto.VoteResultEntry.Detailed)),
            StatusCode.InvalidArgument,
            "detailed result entry is only allowed if exactly one variants ballot exists");
    }

    [Fact]
    public async Task InvalidReportDomainOfInfluenceLevelShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.ReportDomainOfInfluenceLevel = 13)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VoteUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        var id = Guid.Parse(VoteMockedData.IdGossauVoteInContestBund);
        await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
        {
            o.Id = id.ToString();
            o.ContestId = ContestMockedData.IdBundContest;
            o.DomainOfInfluenceId = VoteMockedData.GossauVoteInContestBund.DomainOfInfluenceId.ToString();
            o.Active = VoteMockedData.GossauVoteInContestBund.Active;
            o.ResultAlgorithm = _mapper.Map<SharedProto.VoteResultAlgorithm>(VoteMockedData.GossauVoteInContestBund.ResultAlgorithm);
            o.ReportDomainOfInfluenceLevel = 0;
            o.PoliticalBusinessNumber = "1661new";
        }));

        var ev = EventPublisherMock.GetSinglePublishedEvent<VoteAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var vote = await ElectionAdminClient.GetAsync(new GetVoteRequest
        {
            Id = id.ToString(),
        });
        vote.MatchSnapshot("reponse");

        await AssertHasPublishedEventProcessedMessage(VoteAfterTestingPhaseUpdated.Descriptor, id);
    }

    [Fact]
    public async Task VoteUpdateAfterTestingPhaseJobNotYetRunShouldWork()
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == ContestMockedData.BundContest.Id,
            c => c.EndOfTestingPhase = DateTime.MinValue);

        var id = Guid.Parse(VoteMockedData.IdGossauVoteInContestBund);
        await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
        {
            o.Id = id.ToString();
            o.ContestId = ContestMockedData.IdBundContest;
            o.DomainOfInfluenceId = VoteMockedData.GossauVoteInContestBund.DomainOfInfluenceId.ToString();
            o.Active = VoteMockedData.GossauVoteInContestBund.Active;
            o.ResultAlgorithm = _mapper.Map<SharedProto.VoteResultAlgorithm>(VoteMockedData.GossauVoteInContestBund.ResultAlgorithm);
            o.ReportDomainOfInfluenceLevel = 0;
            o.PoliticalBusinessNumber = "1661new";
        }));

        EventPublisherMock.GetPublishedEvents<VoteAfterTestingPhaseUpdated>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<VoteUpdated>().Should().HaveCount(0);
    }

    [Fact]
    public async Task VoteUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = VoteMockedData.IdGossauVoteInContestBund;
                o.ContestId = ContestMockedData.IdBundContest;
                o.DomainOfInfluenceId = VoteMockedData.GossauVoteInContestBund.DomainOfInfluenceId.ToString();
                o.ReportDomainOfInfluenceLevel = 0;
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task VoteInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Archived);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = VoteMockedData.IdGossauVoteInContestBund;
                o.ContestId = ContestMockedData.IdBundContest;
                o.DomainOfInfluenceId = VoteMockedData.GossauVoteInContestBund.DomainOfInfluenceId.ToString();
                o.ReportDomainOfInfluenceLevel = 0;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
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

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new VoteService.VoteServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    private UpdateVoteRequest NewValidRequest(
        Action<UpdateVoteRequest>? customizer = null)
    {
        var request = new UpdateVoteRequest
        {
            Id = VoteMockedData.IdStGallenVoteInContestStGallen,
            PoliticalBusinessNumber = "1661",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Update Abstimmung") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Upd Abstimmung") },
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            ContestId = ContestMockedData.IdStGallenEvoting,
            Active = false,
            ReportDomainOfInfluenceLevel = 1,
            ResultAlgorithm = SharedProto.VoteResultAlgorithm.CountingCircleMajority,
            ResultEntry = SharedProto.VoteResultEntry.FinalResults,
            AutomaticBallotBundleNumberGeneration = true,
            BallotBundleSampleSizePercent = 20,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Type = SharedProto.VoteType.QuestionsOnSingleBallot,
        };

        customizer?.Invoke(request);
        return request;
    }

    private VoteUpdated NewValidEvent(
        Action<VoteUpdated>? customizer = null)
    {
        var ev = new VoteUpdated
        {
            Vote = new VoteEventData
            {
                Id = VoteMockedData.IdStGallenVoteInContestStGallen,
                PoliticalBusinessNumber = "4000",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Update Abstimmung") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Upd Abstimmung") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContestId = ContestMockedData.IdStGallenEvoting,
                ResultEntry = SharedProto.VoteResultEntry.Detailed,
                AutomaticBallotBundleNumberGeneration = false,
                BallotBundleSampleSizePercent = 20,
                EnforceResultEntryForCountingCircles = true,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                Type = SharedProto.VoteType.QuestionsOnSingleBallot,
            },
        };

        customizer?.Invoke(ev);
        return ev;
    }
}
