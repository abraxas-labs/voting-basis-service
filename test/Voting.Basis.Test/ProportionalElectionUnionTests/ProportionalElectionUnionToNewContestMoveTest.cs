// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionUnionTests;

public class ProportionalElectionUnionToNewContestMoveTest : BaseTest
{
    public ProportionalElectionUnionToNewContestMoveTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestProcessor()
    {
        var pbUnionId = Guid.Parse(ProportionalElectionUnionMockedData.IdKirche);
        var newContestId = Guid.Parse(ContestMockedData.IdBundContest);

        await TestEventPublisher.Publish(
            new ProportionalElectionUnionToNewContestMoved
            {
                ProportionalElectionUnionId = pbUnionId.ToString(),
                NewContestId = newContestId.ToString(),
            });

        var pb = await RunOnDb(db => db.ProportionalElectionUnions.SingleAsync(x => x.Id == pbUnionId));
        pb.ContestId.Should().Be(newContestId);
    }
}
