// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Extensions;

public static class DomainOfInfluenceExtensions
{
    /// <summary>
    /// Checks whether a domain of influence type is a political type.
    /// While types such as <see cref="DomainOfInfluenceType.Sc"/> (Schulgemeinde) are also kinda political,
    /// they do not fit into the usual political hierarchy and are therefore not considered political.
    /// </summary>
    /// <param name="type">The domain of influence type to check.</param>
    /// <returns>Whether the domain of influence type is political.</returns>
    public static bool IsPolitical(this DomainOfInfluenceType type)
        => type is >= DomainOfInfluenceType.Ch and <= DomainOfInfluenceType.Sk;

    /// <summary>
    /// Checks whether a domain of influence type is a communal type.
    /// </summary>
    /// <param name="type">The domain of influence type to check.</param>
    /// <returns>Whether the domain of influence type is communal.</returns>
    public static bool IsCommunal(this DomainOfInfluenceType type)
        => type is >= DomainOfInfluenceType.Mu;
}
