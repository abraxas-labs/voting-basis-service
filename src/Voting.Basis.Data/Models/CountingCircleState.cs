// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

public enum CountingCircleState
{
    Unspecified,

    /// <summary>
    /// counting circle is active.
    /// </summary>
    Active,

    /// <summary>
    /// counting circle is deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// counting circle is merged into an other counting circle.
    /// </summary>
    Merged,

    /// <summary>
    /// counting circle is inactive when it is scheduled by a merge but not activated yet.
    /// </summary>
    Inactive,
}
