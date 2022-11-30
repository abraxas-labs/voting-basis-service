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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.VoteTests;

public class BallotCreateTest : BaseGrpcTest<VoteService.VoteServiceClient>
{
    public BallotCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var ballot = new BallotEventData
        {
            Id = "239702ef-3064-498c-beea-ebf57a55ff05",
            Position = 1,
            VoteId = VoteMockedData.IdStGallenVoteInContestStGallenWithoutChilds,
            BallotType = SharedProto.BallotType.StandardBallot,
            BallotQuestions =
                {
                    new BallotQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Frage 1 neu") },
                    },
                },
        };

        var ballot2 = new BallotEventData
        {
            Id = "259902ef-3064-498c-beea-ebf57a55ecab",
            Position = 2,
            VoteId = VoteMockedData.IdStGallenVoteInContestStGallenWithoutChilds,
            BallotType = SharedProto.BallotType.StandardBallot,
            BallotQuestions =
                {
                    new BallotQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Frage 2 neu") },
                    },
                },
        };

        var ballot3 = new BallotEventData
        {
            Id = "0afc89f8-fc84-4a86-ace6-2cedfb5f8033",
            Position = 3,
            VoteId = VoteMockedData.IdStGallenVoteInContestStGallenWithoutChilds,
            BallotType = SharedProto.BallotType.VariantsBallot,
            HasTieBreakQuestions = true,
            BallotQuestions =
                {
                    new BallotQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Variante 1 neu") },
                    },
                    new BallotQuestionEventData
                    {
                        Number = 2,
                        Question = { LanguageUtil.MockAllLanguages("Variante 2 neu") },
                    },
                    new BallotQuestionEventData
                    {
                        Number = 3,
                        Question = { LanguageUtil.MockAllLanguages("Variante 3 neu") },
                    },
                },
            TieBreakQuestions =
                {
                    new TieBreakQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V2") },
                        Question1Number = 1,
                        Question2Number = 2,
                    },
                    new TieBreakQuestionEventData
                    {
                        Number = 2,
                        Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V3") },
                        Question1Number = 1,
                        Question2Number = 3,
                    },
                    new TieBreakQuestionEventData
                    {
                        Number = 3,
                        Question = { LanguageUtil.MockAllLanguages("TieBreak V2/V3") },
                        Question1Number = 2,
                        Question2Number = 3,
                    },
                },
        };

        await TestEventPublisher.Publish(
            new BallotCreated
            {
                Ballot = ballot,
            },
            new BallotCreated
            {
                Ballot = ballot2,
            },
            new BallotCreated
            {
                Ballot = ballot3,
            });

        var response = await AdminClient.GetAsync(new GetVoteRequest
        {
            Id = VoteMockedData.IdStGallenVoteInContestStGallenWithoutChilds,
        });

        response.MatchSnapshot();
    }

    [Fact]
    public async Task StandardBallotShouldReturnOk()
    {
        var response = await AdminClient.CreateBallotAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<BallotCreated, EventSignatureBusinessMetadata>();

        eventData.Ballot.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", e => e.Ballot.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<BallotCreated>();
        });
    }

    [Fact]
    public async Task StandardBallotWithMultipleQuestionsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateBallotAsync(NewValidRequest(x =>
                x.BallotQuestions.Add(new ProtoModels.BallotQuestion
                {
                    Number = 2,
                    Question = { LanguageUtil.MockAllLanguages("Frage 2") },
                }))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task StandardBallotWithTieBreakQuestionsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateBallotAsync(NewValidRequest(x => x.HasTieBreakQuestions = true)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task StandardBallotWithWrongNumberShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateBallotAsync(NewValidRequest(x => x.BallotQuestions[0].Number = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VariantsBallotShouldReturnOk()
    {
        var response = await AdminClient.CreateBallotAsync(NewValidVariantRequest());

        var eventData = EventPublisherMock.GetSinglePublishedEvent<BallotCreated>();

        eventData.Ballot.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", e => e.Ballot.Id);
    }

    [Fact]
    public async Task VariantsTieBreakBallotShouldReturnOk()
    {
        var response = await AdminClient.CreateBallotAsync(NewValidTieBreakRequest(3));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<BallotCreated>();

        eventData.Ballot.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", e => e.Ballot.Id);
    }

    [Fact]
    public async Task VariantsBallotWithMoreThan3QuestionsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateBallotAsync(NewValidTieBreakRequest(4)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VariantsBallotWithOnlyOneQuestionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateBallotAsync(NewValidRequest(x =>
                x.BallotType = SharedProto.BallotType.VariantsBallot)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateTwiceShouldThrow()
    {
        await AdminClient.CreateBallotAsync(NewValidRequest());

        await AssertStatus(
            async () => await AdminClient.CreateBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Ballot");
    }

    [Fact]
    public async Task BallotInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.CreateBallotAsync(NewValidRequest(x =>
                x.VoteId = VoteMockedData.IdGossauVoteInContestBund)),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new VoteService.VoteServiceClient(channel)
            .CreateBallotAsync(NewValidRequest());

    private CreateBallotRequest NewValidRequest(
        Action<CreateBallotRequest>? customizer = null)
    {
        var request = new CreateBallotRequest
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
                    },
                },
        };

        customizer?.Invoke(request);
        return request;
    }

    private CreateBallotRequest NewValidVariantRequest(
        Action<CreateBallotRequest>? customizer = null)
    {
        var req = NewValidRequest();
        req.BallotType = SharedProto.BallotType.VariantsBallot;
        req.BallotQuestions.Add(new ProtoModels.BallotQuestion
        {
            Number = 2,
            Question = { LanguageUtil.MockAllLanguages("Frage 2") },
        });
        customizer?.Invoke(req);
        return req;
    }

    private CreateBallotRequest NewValidTieBreakRequest(int variantsCount)
    {
        var req = NewValidVariantRequest();
        for (var i = req.BallotQuestions.Count; i < variantsCount; i++)
        {
            req.BallotQuestions.Add(new ProtoModels.BallotQuestion
            {
                Number = i + 1,
                Question = { LanguageUtil.MockAllLanguages($"Frage {i + 1}") },
            });
        }

        req.HasTieBreakQuestions = true;
        for (var i = 0; i < req.BallotQuestions.Count - 1; i++)
        {
            for (var j = i + 1; j < req.BallotQuestions.Count; j++)
            {
                req.TieBreakQuestions.Add(new ProtoModels.TieBreakQuestion
                {
                    Number = req.TieBreakQuestions.Count + 1,
                    Question = { LanguageUtil.MockAllLanguages($"TieBreakFrage {(i + 1) * (j + 1)} ({i + 1} / {j + 1})") },
                    Question1Number = i + 1,
                    Question2Number = j + 1,
                });
            }
        }

        return req;
    }
}
