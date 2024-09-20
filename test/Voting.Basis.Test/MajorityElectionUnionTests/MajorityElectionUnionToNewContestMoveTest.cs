// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test.MajorityElectionUnionTests;

public class MajorityElectionUnionToNewContestMoveTest : BaseTest
{
    public MajorityElectionUnionToNewContestMoveTest(TestApplicationFactory factory)
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
        var pbUnionId = Guid.Parse(MajorityElectionUnionMockedData.IdStGallen2);
        var newContestId = Guid.Parse(ContestMockedData.IdBundContest);

        await TestEventPublisher.Publish(
            new MajorityElectionUnionToNewContestMoved
            {
                MajorityElectionUnionId = pbUnionId.ToString(),
                NewContestId = newContestId.ToString(),
            });

        var pb = await RunOnDb(db => db.MajorityElectionUnions.SingleAsync(x => x.Id == pbUnionId));
        pb.ContestId.Should().Be(newContestId);
    }
}
