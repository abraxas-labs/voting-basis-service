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
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestEVotingApprovalJobTest : BaseTest
{
    public ContestEVotingApprovalJobTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task JobShouldApproveEVoting()
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests
                .AsTracking()
                .FirstAsync(c => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting));
            contest.EVotingApprovalDueDate = MockedClock.GetDate(-3);
            await db.SaveChangesAsync();
        });

        await RunScoped<ApproveContestEVotingJob>(job => job.Run(CancellationToken.None));

        var events = EventPublisherMock.GetPublishedEvents<ContestEVotingApprovalUpdated>().ToList();

        events.Count.Should().Be(1);
        events[0].Approved.Should().BeTrue();
    }

    [Fact]
    public async Task JobWithNoPastEVotingDueDateShouldReturn()
    {
        await RunScoped<ApproveContestEVotingJob>(job => job.Run(CancellationToken.None));
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
            contest.EVotingApprovalDueDate = MockedClock.GetDate(-3);
            contest.State = state;
            await db.SaveChangesAsync();
        });

        await RunScoped<ApproveContestEVotingJob>(job => job.Run(CancellationToken.None));
        var result = EventPublisherMock.AllPublishedEvents.Any();
        result.Should().Be(expectedResult);
    }
}
