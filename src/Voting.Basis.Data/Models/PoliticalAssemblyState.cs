// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;
public enum PoliticalAssemblyState
{
    /// <summary>
    /// Political assembly state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Political assembly takes place in the future or today.
    /// </summary>
    Active,

    /// <summary>
    /// Political assembly has taken place in the past and is locked. Political assembly is immutable.
    /// </summary>
    PastLocked,

    /// <summary>
    /// Political assembly is archived and immutable.
    /// </summary>
    Archived,
}
