// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionToNewContestMoveTest : BaseTest
{
    public MajorityElectionToNewContestMoveTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestProcessor()
    {
        var pbId = Guid.Parse(MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen);
        var newContestId = Guid.Parse(ContestMockedData.IdBundContest);

        await TestEventPublisher.Publish(
            new MajorityElectionToNewContestMoved
            {
                MajorityElectionId = pbId.ToString(),
                NewContestId = newContestId.ToString(),
            });

        var pb = await RunOnDb(db => db.MajorityElections.SingleAsync(x => x.Id == pbId));
        var simplePb = await RunOnDb(db => db.SimplePoliticalBusiness.SingleAsync(x => x.Id == pbId));

        pb.ContestId.Should().Be(newContestId);
        simplePb.ContestId.Should().Be(newContestId);
    }
}
