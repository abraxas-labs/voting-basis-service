// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.Extensions;

public static class DomainOfInfluenceCountingCircleQueryableExtensions
{
    /// <summary>
    /// Gets the inherited DomainOfInfluenceCountingCircles.
    /// </summary>
    /// <param name="query">DomainOfInfluenceCountingCircle queryable.</param>
    /// <returns>Inherited DomainOfInfluenceCountingCircle queryable.</returns>
    public static IQueryable<DomainOfInfluenceCountingCircle> WhereIsInherited(this IQueryable<DomainOfInfluenceCountingCircle> query)
    {
        return query.Where(x => x.DomainOfInfluenceId != x.SourceDomainOfInfluenceId);
    }

    /// <summary>
    /// Gets the non-inherited DomainOfInfluenceCountingCircles.
    /// </summary>
    /// <param name="query">DomainOfInfluenceCountingCircle queryable.</param>
    /// <returns>Non-inherited DomainOfInfluenceCountingCircle queryable.</returns>
    public static IQueryable<DomainOfInfluenceCountingCircle> WhereIsNotInherited(this IQueryable<DomainOfInfluenceCountingCircle> query)
    {
        return query.Where(x => x.DomainOfInfluenceId == x.SourceDomainOfInfluenceId);
    }
}
