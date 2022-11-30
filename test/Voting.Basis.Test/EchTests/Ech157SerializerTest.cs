// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Ech.Converters;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.EchTests;

public class Ech157SerializerTest : BaseEchTest
{
    public Ech157SerializerTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestMajorityElections()
    {
        await MajorityElectionMockedData.Seed(RunScoped);

        var contest = await RunOnDb(async db =>
            await db.Contests
                .AsSplitQuery()
                .Include(c => c.MajorityElections)
                    .ThenInclude(m => m.SecondaryMajorityElections)
                        .ThenInclude(s => s.Candidates)
                .Include(c => c.MajorityElections)
                    .ThenInclude(m => m.MajorityElectionCandidates)
                .FirstAsync(c => c.Id == ContestMockedData.StGallenEvotingContest.Id));

        RunScoped<Ech157Serializer>(serializer =>
        {
            var ech157 = serializer.ToDelivery(contest, contest.MajorityElections);
            var serializedBytes = EchSerializer.ToXml(ech157);
            var serialized = Encoding.UTF8.GetString(serializedBytes);

            XmlUtil.ValidateSchema(serialized, BuildSchemaSet());
            MatchXmlSnapshot(serialized, nameof(TestMajorityElections));
        });
    }

    [Fact]
    public async Task TestProportionalElections()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);

        var contest = await RunOnDb(async db =>
            await db.Contests
                .AsSplitQuery()
                .Include(c => c.ProportionalElections)
                    .ThenInclude(p => p.ProportionalElectionLists)
                        .ThenInclude(l => l.ProportionalElectionCandidates)
                .Include(c => c.ProportionalElections)
                    .ThenInclude(p => p.ProportionalElectionListUnions)
                        .ThenInclude(l => l.ProportionalElectionListUnionEntries)
                .FirstAsync(c => c.Id == ContestMockedData.StGallenEvotingContest.Id));

        RunScoped<Ech157Serializer>(serializer =>
        {
            var ech157 = serializer.ToDelivery(contest, contest.ProportionalElections);
            var serializedBytes = EchSerializer.ToXml(ech157);
            var serialized = Encoding.UTF8.GetString(serializedBytes);

            XmlUtil.ValidateSchema(serialized, BuildSchemaSet());
            MatchXmlSnapshot(serialized, nameof(TestProportionalElections));
        });
    }
}
