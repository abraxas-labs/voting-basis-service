// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

public enum SwissAbroadVotingRight
{
    /// <summary>
    /// Swiss abroad voting right is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Swiss abroad have voting rights on one separate counting circle.
    /// </summary>
    SeparateCountingCircle,

    /// <summary>
    /// Swiss abroad have voting rights on every counting circle.
    /// </summary>
    OnEveryCountingCircle,

    /// <summary>
    /// Swiss abroad don't have any voting rights.
    /// </summary>
    NoRights,
}
