﻿// (c) Copyright by Abraxas Informatik AG
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
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.VoteTests;

public class BallotUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<VoteService.VoteServiceClient>
{
    public BallotUpdateTest(TestApplicationFactory factory)
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
        await TestEventPublisher.Publish(
            new BallotUpdated
            {
                Ballot = NewValidEventData(),
            });

        var response = await ElectionAdminClient.GetAsync(new GetVoteRequest
        {
            Id = VoteMockedData.IdGossauVoteInContestGossau,
        });

        response.MatchSnapshot();

        await RunEvents<BallotUpdated>();
        await AssertHasPublishedEventProcessedMessage(BallotUpdated.Descriptor, Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestGossau));

        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.FirstAsync(x => x.Id == VoteMockedData.GossauVoteInContestGossau.Id));
        simplePb.BusinessSubType.Should().Be(PoliticalBusinessSubType.VoteVariantBallot);
    }

    [Fact]
    public async Task TestAggregateShouldSetDefaultValues()
    {
        await TestEventPublisher.Publish(
            new BallotUpdated
            {
                Ballot = NewValidEventData(x =>
                {
                    foreach (var ballotQuestion in x.BallotQuestions)
                    {
                        ballotQuestion.Type = SharedProto.BallotQuestionType.Unspecified;
                    }
                }),
            });

        var response = await ElectionAdminClient.GetAsync(new GetVoteRequest
        {
            Id = VoteMockedData.IdGossauVoteInContestGossau,
        });

        response.MatchSnapshot();
    }

    [Fact]
    public async Task StandardBallotShouldReturnOk()
    {
        await ElectionAdminClient.UpdateBallotAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<BallotUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ElectionAdminClient.UpdateBallotAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<BallotUpdated>();
        });
    }

    [Fact]
    public async Task StandardBallotWithMultipleQuestionsShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest(x =>
                x.BallotQuestions.Add(new ProtoModels.BallotQuestion
                {
                    Number = 2,
                    Question = { LanguageUtil.MockAllLanguages("Frage 2") },
                    Type = SharedProto.BallotQuestionType.CounterProposal,
                }))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task StandardBallotWithTieBreakQuestionsShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest(x => x.HasTieBreakQuestions = true)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task StandardBallotWithWrongNumberShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest(x => x.BallotQuestions[0].Number = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task StandardBallotNotFoundShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest(x => x.Id = "ad87714e-44b0-4424-b38d-f7c7046994d8")),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task StandardBallotWithWrongTypeShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest(x => x.BallotQuestions[0].Type = SharedProto.BallotQuestionType.CounterProposal)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VariantsBallotShouldReturnOk()
    {
        var request = NewValidVariantRequest();
        await ElectionAdminClient.UpdateBallotAsync(request);

        var eventData = EventPublisherMock.GetSinglePublishedEvent<BallotUpdated>();

        eventData.Ballot.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task VariantsTieBreakBallotShouldReturnOk()
    {
        await ElectionAdminClient.UpdateBallotAsync(NewValidTieBreakRequest(3));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<BallotUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task VariantsBallotWithMoreThan3QuestionsShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidTieBreakRequest(4)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VariantsBallotWithOnlyOneQuestionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest(x =>
                x.BallotType = SharedProto.BallotType.VariantsBallot)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VariantsBallotWithWrongTypesShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidTieBreakRequest(3, x => x.BallotQuestions[0].Type = SharedProto.BallotQuestionType.CounterProposal)),
            StatusCode.InvalidArgument);

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidTieBreakRequest(3, x => x.BallotQuestions[1].Type = SharedProto.BallotQuestionType.MainBallot)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task QuestionsOnMultipleBallotsShouldWork()
    {
        await ZurichCantonAdminClient.UpdateBallotAsync(new UpdateBallotRequest
        {
            Id = VoteMockedData.BallotId2ZurichVoteInContestZurich,
            VoteId = VoteMockedData.IdZurichVoteInContestZurich,
            BallotType = SharedProto.BallotType.StandardBallot,
            SubType = SharedProto.BallotSubType.Variant1,
            ShortDescription = { LanguageUtil.MockAllLanguages("Variante") },
            OfficialDescription = { LanguageUtil.MockAllLanguages("Abstimmung Variante") },
            BallotQuestions =
            {
                new ProtoModels.BallotQuestion
                {
                    Number = 1,
                    Question = { LanguageUtil.MockAllLanguages("Variante") },
                    Type = SharedProto.BallotQuestionType.MainBallot,
                },
            },
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<BallotUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task BallotUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await ElectionAdminClient.UpdateBallotAsync(new UpdateBallotRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestBund,
            Id = VoteMockedData.BallotIdGossauVoteInContestBund,
            BallotType = SharedProto.BallotType.VariantsBallot,
            BallotQuestions =
                {
                    new ProtoModels.BallotQuestion
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Frage 1 Gossau (geändert)") },
                        Type = SharedProto.BallotQuestionType.MainBallot,
                    },
                    new ProtoModels.BallotQuestion
                    {
                        Number = 2,
                        Question = { LanguageUtil.MockAllLanguages("Frage 2 Gossau (geändert)") },
                        Type = SharedProto.BallotQuestionType.Variant,
                    },
                },
            HasTieBreakQuestions = true,
            TieBreakQuestions =
                {
                    new ProtoModels.TieBreakQuestion
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Stichfrage 1 Gossau (Frage 1 vs Frage 2), geändert") },
                        Question1Number = 1,
                        Question2Number = 2,
                    },
                },
        });

        var ev = EventPublisherMock.GetSinglePublishedEvent<BallotAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var response = await ElectionAdminClient.GetAsync(new GetVoteRequest
        {
            Id = VoteMockedData.IdGossauVoteInContestBund,
        });
        response.MatchSnapshot("response");
    }

    [Fact]
    public async Task BallotUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(new UpdateBallotRequest
            {
                VoteId = VoteMockedData.IdGossauVoteInContestBund,
                Id = VoteMockedData.BallotIdGossauVoteInContestBund,
                BallotType = SharedProto.BallotType.StandardBallot,
                BallotQuestions =
                {
                        new ProtoModels.BallotQuestion
                        {
                            Number = 1,
                            Question = { LanguageUtil.MockAllLanguages("Frage 1 Gossau (geändert)") },
                            Type = SharedProto.BallotQuestionType.MainBallot,
                        },
                },
                HasTieBreakQuestions = false,
            }),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task UpdateBallotTypeToStandardForDetailedEntryShouldThrow()
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
                        Type = SharedProto.BallotQuestionType.Variant,
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
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "detailed result entry is only allowed if exactly one variants ballot exists");
    }

    [Fact]
    public async Task BallotInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateBallotAsync(NewValidRequest(x =>
            {
                x.VoteId = VoteMockedData.IdGossauVoteInContestBund;
                x.Id = VoteMockedData.BallotIdGossauVoteInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new VoteService.VoteServiceClient(channel)
            .UpdateBallotAsync(NewValidRequest());

    private UpdateBallotRequest NewValidRequest(
        Action<UpdateBallotRequest>? customizer = null)
    {
        var request = new UpdateBallotRequest
        {
            Id = VoteMockedData.BallotIdStGallenVoteInContestStGallen,
            VoteId = VoteMockedData.IdStGallenVoteInContestStGallen,
            BallotType = SharedProto.BallotType.StandardBallot,
            BallotQuestions =
                {
                    new ProtoModels.BallotQuestion
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Frage 1") },
                        Type = SharedProto.BallotQuestionType.MainBallot,
                        FederalIdentification = 29348929,
                    },
                },
        };

        customizer?.Invoke(request);
        return request;
    }

    private UpdateBallotRequest NewValidVariantRequest(
        Action<UpdateBallotRequest>? customizer = null)
    {
        var req = NewValidRequest();
        req.BallotType = SharedProto.BallotType.VariantsBallot;
        req.BallotQuestions.Add(new ProtoModels.BallotQuestion
        {
            Number = 2,
            Question = { LanguageUtil.MockAllLanguages("Frage 2") },
            Type = SharedProto.BallotQuestionType.Variant,
        });
        customizer?.Invoke(req);
        return req;
    }

    private UpdateBallotRequest NewValidTieBreakRequest(int variantsCount, Action<UpdateBallotRequest>? customizer = null)
    {
        var req = NewValidVariantRequest();
        for (var i = req.BallotQuestions.Count; i < variantsCount; i++)
        {
            req.BallotQuestions.Add(new ProtoModels.BallotQuestion
            {
                Number = i + 1,
                Question = { LanguageUtil.MockAllLanguages($"Frage {i + 1}") },
                Type = SharedProto.BallotQuestionType.Variant,
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

        customizer?.Invoke(req);
        return req;
    }

    private BallotEventData NewValidEventData(Action<BallotEventData>? customizer = null)
    {
        var eventData = new BallotEventData
        {
            Id = VoteMockedData.BallotIdGossauVoteInContestGossau,
            VoteId = VoteMockedData.IdGossauVoteInContestGossau,
            BallotType = SharedProto.BallotType.VariantsBallot,
            HasTieBreakQuestions = true,
            BallotQuestions =
            {
                new BallotQuestionEventData
                {
                    Number = 1,
                    Question = { LanguageUtil.MockAllLanguages("Frage 1 update") },
                    Type = SharedProto.BallotQuestionType.MainBallot,
                },
                new BallotQuestionEventData
                {
                    Number = 2,
                    Question = { LanguageUtil.MockAllLanguages("Frage 2 update") },
                    Type = SharedProto.BallotQuestionType.CounterProposal,
                },
            },
            TieBreakQuestions =
            {
                new TieBreakQuestionEventData
                {
                    Number = 1,
                    Question = { LanguageUtil.MockAllLanguages("TieBreak 1 Updated") },
                    Question1Number = 1,
                    Question2Number = 2,
                },
            },
        };

        customizer?.Invoke(eventData);
        return eventData;
    }
}
