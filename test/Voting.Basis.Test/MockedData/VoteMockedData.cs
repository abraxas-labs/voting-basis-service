// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.EventProcessors;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Utils;

namespace Voting.Basis.Test.MockedData;

public static class VoteMockedData
{
    public const string IdBundVoteInContestBund = "7550e7c2-27e3-4602-a928-6f9c2afd2289";
    public const string IdStGallenVoteInContestBund = "903a47bf-6b7b-4460-8475-b7bd89ea2ac9";
    public const string IdUzwilVoteInContestBund = "6b4c7f11-9860-4468-ace9-78baec913a8d";
    public const string IdBundVoteInContestStGallen = "f69b3543-ccee-467d-9cde-56941f6e4bad";
    public const string IdGossauVoteInContestStGallen = "8076dee2-f19b-4af9-80b1-69b0c7b1402b";
    public const string IdGossauVoteInContestBund = "76d9b4bd-52e6-462e-9613-f344f7b860df";
    public const string IdUzwilVoteInContestStGallen = "da65e354-f668-4ae4-b3ef-c1a74764e99d";
    public const string IdStGallenVoteInContestStGallen = "96d8275f-f1f8-4933-a097-5c0c19f54567";
    public const string IdStGallenVoteInContestStGallenWithoutChilds = "607a9dbc-250a-4bbf-ab31-73c81f6556ba";
    public const string IdGossauVoteInContestGossau = "8fbba43a-cd73-407a-b490-df13c41cc5ee";
    public const string IdUzwilVoteInContestUzwilWithoutChilds = "7de846be-9e60-45c7-81e3-51441ff37592";
    public const string IdGenfVoteInContestBundWithoutChilds = "b751e349-0d2c-482c-b5f9-780608cca9f8";
    public const string IdKircheVoteInContestKircheWithoutChilds = "bfd3a5ba-a9e5-4cdd-9b81-16a181cf53cb";

    public const string BallotIdBundVoteInContestBund = "b7ba9fef-27f9-46c4-8046-955e874561a7";
    public const string BallotIdStGallenVoteInContestBund = "60dd6c2c-e73a-467e-99e1-902f973a5d8e";
    public const string BallotIdBundVoteInContestStGallen = "512bb3a2-97e2-4779-ac51-83abd039afc4";
    public const string BallotIdGossauVoteInContestStGallen = "154ee710-88b7-419a-ae5b-74a44c9c969e";
    public const string BallotIdGossauVoteInContestBund = "690c36d8-dd09-4464-91ce-73c48df176a1";
    public const string BallotIdUzwilVoteInContestStGallen = "e6ee82f9-70d4-4ffa-a673-34d56fc47204";
    public const string BallotIdStGallenVoteInContestStGallen = "0ed26aad-e169-4d55-b9a9-90475ba81a02";
    public const string BallotIdGossauVoteInContestGossau = "a2aa6f61-752b-446f-94b4-002a95111c7a";
    public const string BallotIdUzwilVoteInContestUzwil = "7ad05ec9-0ad9-4490-8b97-368f7175a7f0";

    public static Vote BundVoteInContestBund
        => new Vote
        {
            Id = Guid.Parse(IdBundVoteInContestBund),
            PoliticalBusinessNumber = "200",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Bund"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Bund"),
            InternalDescription = "Abstimmung Bund auf Urnengang Bund",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            ContestId = ContestMockedData.BundContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.CountingCircleMajority,
            Active = true,
            BallotBundleSampleSizePercent = 25,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdBundVoteInContestBund),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("21507485-971a-4a1d-b1ac-5b153d0e3082"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote StGallenVoteInContestBund
        => new Vote
        {
            Id = Guid.Parse(IdStGallenVoteInContestBund),
            PoliticalBusinessNumber = "201",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung St. Gallen"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung SG"),
            InternalDescription = "Abstimmung St. Gallen auf Urnengang Bund",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            ContestId = ContestMockedData.BundContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.CountingCircleUnanimity,
            Active = true,
            BallotBundleSampleSizePercent = 0,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdStGallenVoteInContestBund),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("35e3ba5c-4b03-485e-a816-b11590b34f90"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote BundVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdBundVoteInContestStGallen),
            PoliticalBusinessNumber = "100",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Bund"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Bund"),
            InternalDescription = "Abstimmung Bund auf Urnengang St.Gallen",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = true,
            BallotBundleSampleSizePercent = 25,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdBundVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("b86d3fb0-0a01-46cd-a616-c559bb17c57b"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 St.Gallen"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote UzwilVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdUzwilVoteInContestStGallen),
            PoliticalBusinessNumber = "166",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Uzwil"),
            InternalDescription = "Abstimmung Uzwil auf Urnengang St.Gallen",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.CountingCircleMajority,
            Active = true,
            BallotBundleSampleSizePercent = 10,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdUzwilVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("191761e9-c08a-4d36-b693-cd9ec56f73ca"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 Uzwil"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote StGallenVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdStGallenVoteInContestStGallen),
            PoliticalBusinessNumber = "155",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung St.Gallen"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung St.Gallen"),
            InternalDescription = "Abstimmung St.Gallen auf Urnengang St.Gallen",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.CountingCircleUnanimity,
            Active = true,
            ReportDomainOfInfluenceLevel = 1,
            BallotBundleSampleSizePercent = 50,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdStGallenVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("49e72672-bbaf-4724-ab82-ce19305f2be4"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 St.Gallen"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote GossauVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdGossauVoteInContestStGallen),
            PoliticalBusinessNumber = "321",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Gossau"),
            InternalDescription = "Abstimmung Gossau auf Urnengang St.Gallen",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = true,
            BallotBundleSampleSizePercent = 25,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdGossauVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.VariantsBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("09a399a8-5698-4d26-81a8-3e37dc1b37ac"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 Gossau"),
                                Type = BallotQuestionType.MainBallot,
                            },
                            new BallotQuestion
                            {
                                Number = 2,
                                Id = Guid.Parse("09eda08b-fc81-4368-879a-97fdece5924d"),
                                Question = LanguageUtil.MockAllLanguages("Frage 2 Gossau"),
                                Type = BallotQuestionType.CounterProposal,
                            },
                        },
                        HasTieBreakQuestions = true,
                        TieBreakQuestions = new List<TieBreakQuestion>
                        {
                            new TieBreakQuestion
                            {
                                Id = Guid.Parse("834b6371-7fa6-4062-82aa-e8a19ec28ff0"),
                                Number = 1,
                                Question = LanguageUtil.MockAllLanguages("Stichfrage 1 Gossau (Frage 1 vs Frage 2)"),
                                Question1Number = 1,
                                Question2Number = 2,
                            },
                        },
                    },
            },
        };

    public static Vote GossauVoteInContestBund
        => new Vote
        {
            Id = Guid.Parse(IdGossauVoteInContestBund),
            PoliticalBusinessNumber = "4531",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Gossau"),
            InternalDescription = "Abstimmung Gossau",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.BundContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = true,
            BallotBundleSampleSizePercent = 0,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdGossauVoteInContestBund),
                        Position = 1,
                        BallotType = BallotType.VariantsBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("abf6327a-53ec-4901-9a59-de642b20e67c"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 Gossau"),
                                Type = BallotQuestionType.MainBallot,
                            },
                            new BallotQuestion
                            {
                                Number = 2,
                                Id = Guid.Parse("fb0e5418-be49-4ea1-b777-b9ad3a2999d2"),
                                Question = LanguageUtil.MockAllLanguages("Frage 2 Gossau"),
                                Type = BallotQuestionType.Variant,
                            },
                        },
                        HasTieBreakQuestions = true,
                        TieBreakQuestions = new List<TieBreakQuestion>
                        {
                          new TieBreakQuestion
                          {
                              Id = Guid.Parse("ccaeffeb-653b-46c1-8e5e-8eeb45dddcab"),
                              Number = 1,
                              Question = LanguageUtil.MockAllLanguages("Stichfrage 1 Gossau (Frage 1 vs Frage 2)"),
                              Question1Number = 1,
                              Question2Number = 2,
                          },
                        },
                    },
            },
        };

    public static Vote StGallenVoteInContestStGallenWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdStGallenVoteInContestStGallenWithoutChilds),
            PoliticalBusinessNumber = "500",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung St.Gallen 2"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung St.Gallen 2"),
            InternalDescription = "Abstimmung St.Gallen auf Urnengang St.Gallen ohne Vorlage und Optionen",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            ContestId = ContestMockedData.StGallenEvotingContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = false,
            BallotBundleSampleSizePercent = 10,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static Vote GossauVoteInContestGossau
        => new Vote
        {
            Id = Guid.Parse(IdGossauVoteInContestGossau),
            PoliticalBusinessNumber = "324",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Gossau"),
            InternalDescription = "Abstimmung Gossau auf Urnengang Gossau mit E-Voting",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            ContestId = ContestMockedData.GossauContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = true,
            BallotBundleSampleSizePercent = 100,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdGossauVoteInContestGossau),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("f1344734-0ac3-4c1b-aafe-c39bfb2f277b"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 Gossau"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote UzwilVoteInContestUzwil
        => new Vote
        {
            Id = Guid.Parse(IdUzwilVoteInContestUzwilWithoutChilds),
            PoliticalBusinessNumber = "412",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Uzwil"),
            InternalDescription = "Abstimmung Uzwil auf Urnengang Uzwil",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            ContestId = ContestMockedData.UzwilEvotingContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = true,
            BallotBundleSampleSizePercent = 25,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdUzwilVoteInContestUzwil),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("328188ad-0a94-4c7b-8391-0ad3d5738286"),
                                Question = LanguageUtil.MockAllLanguages("Frage 1 Uzwil"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote UzwilVoteInContestBundWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdUzwilVoteInContestBund),
            PoliticalBusinessNumber = "714",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Uzwil"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Uzwil"),
            InternalDescription = "Abstimmung Uzwil auf Urnengang Bund",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            Active = false,
            ContestId = ContestMockedData.BundContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            BallotBundleSampleSizePercent = 10,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static Vote GenfVoteInContestBundWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdGenfVoteInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714a",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Genf"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Genf"),
            InternalDescription = "Abstimmung Genf auf Urnengang Bund",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGenf,
            ContestId = ContestMockedData.BundContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = false,
            BallotBundleSampleSizePercent = 25,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static Vote KircheVoteInContestKircheWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdKircheVoteInContestKircheWithoutChilds),
            PoliticalBusinessNumber = "aaa",
            OfficialDescription = LanguageUtil.MockAllLanguages("Abstimmung Kirche"),
            ShortDescription = LanguageUtil.MockAllLanguages("Abstimmung Kirche"),
            InternalDescription = "Abstimmung Kirche auf Urnengang Kirche",
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidKirchgemeinde,
            ContestId = ContestMockedData.KirchenContest.Id,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            Active = false,
            BallotBundleSampleSizePercent = 45,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Physically,
            EnforceReviewProcedureForCountingCircles = false,
        };

    public static IEnumerable<Vote> All
    {
        get
        {
            yield return BundVoteInContestBund;
            yield return BundVoteInContestStGallen;
            yield return UzwilVoteInContestStGallen;
            yield return StGallenVoteInContestStGallen;
            yield return GossauVoteInContestStGallen;
            yield return GossauVoteInContestBund;
            yield return StGallenVoteInContestStGallenWithoutChilds;
            yield return GossauVoteInContestGossau;
            yield return UzwilVoteInContestUzwil;
            yield return UzwilVoteInContestBundWithoutChilds;
            yield return GenfVoteInContestBundWithoutChilds;
            yield return KircheVoteInContestKircheWithoutChilds;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, bool seedContest = true)
    {
        if (seedContest)
        {
            await ContestMockedData.Seed(runScoped);
        }

        await runScoped(async sp =>
        {
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<Vote>>();
            var db = sp.GetRequiredService<DataContext>();
            db.Votes.AddRange(All);
            await db.SaveChangesAsync();

            foreach (var vote in All)
            {
                await simplePbBuilder.Create(vote);
            }

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var voteAggregates = All.Select(v => ToAggregate(v, aggregateFactory, mapper));

            foreach (var vote in voteAggregates)
            {
                await aggregateRepository.Save(vote);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });
    }

    private static VoteAggregate ToAggregate(
        Vote vote,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<VoteAggregate>();
        var domainVote = mapper.Map<Core.Domain.Vote>(vote);
        var domainBallots = mapper.Map<IEnumerable<Core.Domain.Ballot>>(vote.Ballots);

        var setEnforceFalse = !vote.EnforceResultEntryForCountingCircles;
        var setDetailed = vote.ResultEntry == VoteResultEntry.Detailed;
        domainVote.EnforceResultEntryForCountingCircles = true;
        domainVote.ResultEntry = VoteResultEntry.FinalResults;
        aggregate.CreateFrom(domainVote);

        foreach (var ballot in domainBallots)
        {
            ballot.VoteId = domainVote.Id;
            aggregate.CreateBallot(ballot);
        }

        if (setEnforceFalse || setDetailed)
        {
            domainVote.EnforceResultEntryForCountingCircles = !setEnforceFalse;
            domainVote.ResultEntry = setDetailed
                ? VoteResultEntry.Detailed
                : VoteResultEntry.FinalResults;
            aggregate.UpdateFrom(domainVote);
        }

        return aggregate;
    }
}
