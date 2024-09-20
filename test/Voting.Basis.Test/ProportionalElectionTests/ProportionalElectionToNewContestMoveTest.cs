// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionToNewContestMoveTest : BaseTest
{
    public ProportionalElectionToNewContestMoveTest(TestApplicationFactory factory)
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
        var pbId = Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestStGallen);
        var newContestId = Guid.Parse(ContestMockedData.IdBundContest);

        await TestEventPublisher.Publish(
            new ProportionalElectionToNewContestMoved
            {
                ProportionalElectionId = pbId.ToString(),
                NewContestId = newContestId.ToString(),
            });

        var pb = await RunOnDb(db => db.ProportionalElections.SingleAsync(x => x.Id == pbId));
        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.SingleAsync(x => x.Id == pbId));

        pb.ContestId.Should().Be(newContestId);
        simplePb.ContestId.Should().Be(newContestId);
    }
}
