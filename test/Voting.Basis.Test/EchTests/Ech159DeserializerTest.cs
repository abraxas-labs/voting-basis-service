// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using eCH_0159_4_0;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Ech.Converters;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.EchTests;

public class Ech159DeserializerTest : BaseTest
{
    public Ech159DeserializerTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestSerializeDeserialize()
    {
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

        var serialized = RunScoped<Ech159Serializer, Delivery>(serializer => serializer.ToEventInitialDelivery(contest, contest.Votes));

        RunScoped<Ech159Deserializer>(deserializer =>
        {
            var deserialized = deserializer.FromEventInitialDelivery(serialized);
            deserialized.Votes = deserialized.Votes
                .OrderBy(v => v.PoliticalBusinessNumber)
                .ToList();

            foreach (var vote in deserialized.Votes)
            {
                vote.Id = Guid.Empty;

                foreach (var ballot in vote.Ballots)
                {
                    ballot.Id = Guid.Empty;
                    ballot.VoteId = Guid.Empty;

                    foreach (var question in ballot.BallotQuestions)
                    {
                        question.Id = Guid.Empty;
                        question.BallotId = Guid.Empty;
                    }

                    foreach (var question in ballot.TieBreakQuestions)
                    {
                        question.Id = Guid.Empty;
                        question.BallotId = Guid.Empty;
                    }
                }
            }

            deserialized.Id = Guid.Empty;
            deserialized.EndOfTestingPhase = DateTime.MinValue;
            deserialized.MatchSnapshot();
        });
    }
}
