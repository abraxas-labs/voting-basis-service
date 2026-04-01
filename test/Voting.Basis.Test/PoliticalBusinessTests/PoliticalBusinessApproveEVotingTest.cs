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

        var voteActiveStateEvs = EventPublisherMock.GetPublishedEvents<VoteActiveStateUpdated>();
        var voteEVotingEvs = EventPublisherMock.GetPublishedEvents<VoteEVotingApprovalUpdated>();
        var peActiveStateEvs = EventPublisherMock.GetPublishedEvents<ProportionalElectionActiveStateUpdated>();
        var peEVotingEvs = EventPublisherMock.GetPublishedEvents<ProportionalElectionEVotingApprovalUpdated>();
        var meActiveStateEvs = EventPublisherMock.GetPublishedEvents<MajorityElectionActiveStateUpdated>();
        var meEVotingEvs = EventPublisherMock.GetPublishedEvents<MajorityElectionEVotingApprovalUpdated>();
        var smeActiveStateEvs = EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionActiveStateUpdated>();
        var smeEVotingEvs = EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionEVotingApprovalUpdated>();

        voteActiveStateEvs.Count().Should().Be(0);
        voteEVotingEvs.Count().Should().Be(3);
        voteEVotingEvs.All(x => x.Approved).Should().BeTrue();
        peActiveStateEvs.Count().Should().Be(0);
        peEVotingEvs.Count().Should().Be(3);
        peEVotingEvs.All(x => x.Approved).Should().BeTrue();
        meActiveStateEvs.Count().Should().Be(1);
        meActiveStateEvs.All(x => x.Active).Should().BeTrue();
        meEVotingEvs.Count().Should().Be(4);
        meEVotingEvs.All(x => x.Approved).Should().BeTrue();
        smeActiveStateEvs.Count().Should().Be(1);
        smeActiveStateEvs.All(x => x.Active).Should().BeTrue();
        smeEVotingEvs.Count().Should().Be(1);
        smeEVotingEvs.All(x => x.Approved).Should().BeTrue();
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
