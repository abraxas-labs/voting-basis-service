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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.VoteTests;

public class BallotDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<VoteService.VoteServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestBallotId;

    public BallotDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await ElectionAdminClient.DeleteBallotAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<BallotDeleted, EventSignatureBusinessMetadata>();

        eventData.BallotId.Should().Be(VoteMockedData.BallotIdStGallenVoteInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestMultipleBallots()
    {
        await ZurichCantonAdminClient.DeleteBallotAsync(new DeleteBallotRequest
        {
            Id = VoteMockedData.BallotId3ZurichVoteInContestZurich,
            VoteId = VoteMockedData.IdZurichVoteInContestZurich,
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<BallotDeleted>();

        eventData.BallotId.Should().Be(VoteMockedData.BallotId3ZurichVoteInContestZurich);
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ElectionAdminClient.DeleteBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<BallotDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = VoteMockedData.BallotIdGossauVoteInContestStGallen;
        var idGuid = Guid.Parse(id);
        await TestEventPublisher.Publish(new BallotDeleted { BallotId = id });

        (await RunOnDb(db => db.Ballots.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);
        (await RunOnDb(db => db.TieBreakQuestions.CountAsync(tbq => tbq.BallotId == idGuid)))
            .Should().Be(0);
        (await RunOnDb(db => db.BallotQuestions.CountAsync(bq => bq.BallotId == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task NotExistingBallotShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteBallotAsync(NewValidRequest(b => b.Id = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task BallotInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteBallotAsync(NewValidRequest(b =>
            {
                b.VoteId = VoteMockedData.IdGossauVoteInContestBund;
                b.Id = VoteMockedData.BallotIdGossauVoteInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task DeleteSingleVariantsBallotForDetailedEntryShouldThrow()
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
        await ElectionAdminClient.UpdateAsync(new UpdateVoteRequest
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
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            AutomaticBallotBundleNumberGeneration = true,
            BallotBundleSampleSizePercent = 20,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Type = SharedProto.VoteType.QuestionsOnSingleBallot,
        });
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "detailed result entry is only allowed if exactly one variants ballot exists");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestBallotId == null)
        {
            var response = await ElectionAdminClient.CreateBallotAsync(new CreateBallotRequest
            {
                VoteId = VoteMockedData.IdStGallenVoteInContestStGallenWithoutChilds,
                Position = 1,
                BallotType = SharedProto.BallotType.StandardBallot,
                BallotQuestions =
                {
                    new ProtoModels.BallotQuestion
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Frage 1") },
                        Type = SharedProto.BallotQuestionType.MainBallot,
                    },
                },
            });

            _authTestBallotId = response.Id;
        }

        await new VoteService.VoteServiceClient(channel)
            .DeleteBallotAsync(new DeleteBallotRequest
            {
                Id = _authTestBallotId,
                VoteId = VoteMockedData.IdStGallenVoteInContestStGallenWithoutChilds,
            });
        _authTestBallotId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private DeleteBallotRequest NewValidRequest(
        Action<DeleteBallotRequest>? customizer = null)
    {
        var request = new DeleteBallotRequest
        {
            Id = VoteMockedData.BallotIdStGallenVoteInContestStGallen,
            VoteId = VoteMockedData.IdStGallenVoteInContestStGallen,
        };

        customizer?.Invoke(request);
        return request;
    }
}
