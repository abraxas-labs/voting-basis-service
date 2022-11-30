// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

public static class ContestStateExtensions
{
    public static bool TestingPhaseEnded(this ContestState state) => state > ContestState.TestingPhase;

    public static bool IsLocked(this ContestState state) => state is ContestState.PastLocked or ContestState.Archived;
}
