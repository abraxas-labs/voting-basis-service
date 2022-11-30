// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.EchTests;

public class Ech159SerializerTest : BaseEchTest
{
    public Ech159SerializerTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestVotes()
    {
        // Add a new vote, so we can test that votes with the same DOI are correctly grouped together
        await RunOnDb(async db =>
        {
            db.Votes.Add(
                new Vote
                {
                    Id = Guid.Parse("dd520fe0-470d-4a44-8704-13b81735041d"),
                    PoliticalBusinessNumber = "255",
                    OfficialDescription = LanguageUtil.MockAllLanguages("Zusätzliche Abstimmung St.Gallen"),
                    ShortDescription = LanguageUtil.MockAllLanguages("Zusatzabst. St.Gallen"),
                    InternalDescription = "Zusätzliche Abstimmung St.Gallen auf Urnengang St.Gallen",
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
                    ContestId = ContestMockedData.StGallenEvotingContest.Id,
                    ResultAlgorithm = VoteResultAlgorithm.CountingCircleUnanimity,
                    Active = true,
                    ReportDomainOfInfluenceLevel = 1,
                    BallotBundleSampleSizePercent = 25,
                    AutomaticBallotBundleNumberGeneration = false,
                    ResultEntry = VoteResultEntry.FinalResults,
                    EnforceResultEntryForCountingCircles = true,
                    ReviewProcedure = VoteReviewProcedure.Electronically,
                    EnforceReviewProcedureForCountingCircles = true,
                    Ballots = new List<Ballot>
                    {
                        new Ballot
                        {
                            Id = Guid.Parse("d8e5e6eb-99f2-4387-85d6-1b22336f5037"),
                            Position = 1,
                            Description = LanguageUtil.MockAllLanguages("Ballot desc"),
                            BallotType = BallotType.VariantsBallot,
                            BallotQuestions = new List<BallotQuestion>
                            {
                                new BallotQuestion
                                {
                                    Number = 1,
                                    Id = Guid.Parse("7b3270e4-17df-40ff-a5f7-ea1ecc149883"),
                                    Question = LanguageUtil.MockAllLanguages("Frage 1"),
                                },
                                new BallotQuestion
                                {
                                    Number = 2,
                                    Id = Guid.Parse("9691cae5-d046-4813-8cd2-273bc762f106"),
                                    Question = LanguageUtil.MockAllLanguages("Frage 2"),
                                },
                            },
                            HasTieBreakQuestions = true,
                            TieBreakQuestions = new List<TieBreakQuestion>
                            {
                                new TieBreakQuestion
                                {
                                    Id = Guid.Parse("045cd390-d285-417c-b01f-bb907629426e"),
                                    Number = 1,
                                    Question1Number = 1,
                                    Question2Number = 2,
                                    Question = LanguageUtil.MockAllLanguages("Stichfrage"),
                                },
                            },
                        },
                    },
                });
            await db.SaveChangesAsync();
        });

        var contest = await RunOnDb(async db =>
            await db.Contests
                .AsSplitQuery()
                .Include(c => c.Votes)
                    .ThenInclude(v => v.Ballots)
                        .ThenInclude(b => b.BallotQuestions)
                .Include(c => c.Votes)
                    .ThenInclude(v => v.Ballots)
                        .ThenInclude(b => b.TieBreakQuestions)
                .Include(c => c.Votes)
                    .ThenInclude(v => v.DomainOfInfluence)
                .FirstAsync(c => c.Id == ContestMockedData.StGallenEvotingContest.Id));

        RunScoped<Ech159Serializer>(serializer =>
        {
            var ech159 = serializer.ToEventInitialDelivery(contest, contest.Votes);
            var serializedBytes = EchSerializer.ToXml(ech159);
            var serialized = Encoding.UTF8.GetString(serializedBytes);

            XmlUtil.ValidateSchema(serialized, BuildSchemaSet());
            MatchXmlSnapshot(serialized, nameof(TestVotes));
        });
    }
}
