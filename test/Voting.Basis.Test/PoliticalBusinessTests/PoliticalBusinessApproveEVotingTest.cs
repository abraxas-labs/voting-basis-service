// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test.PoliticalBusinessTests;

public class PoliticalBusinessApproveEVotingTest : BaseTest
{
    public PoliticalBusinessApproveEVotingTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await VoteMockedData.Seed(RunScoped, false);
    }

    [Fact]
    public async Task JobShouldApproveEVotingForRemainingPoliticalBusinesses()
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests
                .AsTracking()
                .FirstAsync(c => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting));
            contest.EVotingApproved = true;
            await db.SaveChangesAsync();
        });

        await RunScoped<ApprovePoliticalBusinessEVotingJob>(job => job.Run(CancellationToken.None));

        var voteEvs = EventPublisherMock.GetPublishedEvents<VoteEVotingApprovalUpdated>();
        var peEvs = EventPublisherMock.GetPublishedEvents<ProportionalElectionEVotingApprovalUpdated>();
        var meEvs = EventPublisherMock.GetPublishedEvents<MajorityElectionEVotingApprovalUpdated>();
        var smeEvs = EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionEVotingApprovalUpdated>();

        voteEvs.Count().Should().Be(3);
        voteEvs.All(x => x.Approved).Should().BeTrue();
        peEvs.Count().Should().Be(4);
        peEvs.All(x => x.Approved).Should().BeTrue();
        meEvs.Count().Should().Be(4);
        meEvs.All(x => x.Approved).Should().BeTrue();
        smeEvs.Count().Should().Be(1);
        smeEvs.All(x => x.Approved).Should().BeTrue();
    }

    [Fact]
    public async Task JobWhereContestIsNotEVotingApprovedShouldReturn()
    {
        await RunScoped<ApprovePoliticalBusinessEVotingJob>(job => job.Run(CancellationToken.None));
        EventPublisherMock.AllPublishedEvents.Any().Should().BeFalse();
    }

    [Theory]
    [InlineData(ContestState.TestingPhase, true)]
    [InlineData(ContestState.Active, true)]
    [InlineData(ContestState.PastUnlocked, true)]
    [InlineData(ContestState.PastLocked, false)]
    [InlineData(ContestState.Archived, false)]
    public async Task TestState(ContestState state, bool expectedResult)
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests
                .AsTracking()
                .FirstAsync(c => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting));
            contest.EVotingApproved = true;
            contest.State = state;
            await db.SaveChangesAsync();
        });

        await RunScoped<ApprovePoliticalBusinessEVotingJob>(job => job.Run(CancellationToken.None));
        var result = EventPublisherMock.AllPublishedEvents.Any();
        result.Should().Be(expectedResult);
    }
}
