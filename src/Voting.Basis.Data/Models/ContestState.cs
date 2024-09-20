// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

public enum ContestState
{
    /// <summary>
    /// Contest state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Contest is in testing phase. During the testing phase, users are allowed to enter mock data, which will be cleaned up
    /// at the end of the testing phase. This is used to test the system, including surrounding systems (eg. does the publishing of an export work?).
    /// </summary>
    TestingPhase,

    /// <summary>
    /// Contest takes place in the future or today, but is not in the testing phase anymore.
    /// </summary>
    Active,

    /// <summary>
    /// Contest has taken place in the past and is locked. Contest is immutable until the contest is unlocked (see <see cref="PastUnlocked"/>).
    /// </summary>
    PastLocked,

    /// <summary>
    /// Contest has taken place in the past and is unlocked, but it will automatically get locked after the day ends.
    /// </summary>
    PastUnlocked,

    /// <summary>
    /// Contest is archived and immutable.
    /// </summary>
    Archived,
}
