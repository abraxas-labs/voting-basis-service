// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Basis.Data.Models;
using Xunit;
using DataContestState = Voting.Basis.Data.Models.ContestState;

namespace Voting.Basis.Test.UtilTests;

public class ContestStateExtensionsTest
{
    [Theory]
    [InlineData(DataContestState.Unspecified, false)]
    [InlineData(DataContestState.TestingPhase, false)]
    [InlineData(DataContestState.Active, false)]
    [InlineData(DataContestState.PastLocked, true)]
    [InlineData(DataContestState.PastUnlocked, false)]
    [InlineData(DataContestState.Archived, true)]
    public void TestIsLocked(DataContestState state, bool expectedResult)
    {
        state.IsLocked().Should().Be(expectedResult);
    }
}
