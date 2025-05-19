// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class DomainOfInfluenceCountingCircleInheritanceBuilder
{
    private readonly DomainOfInfluenceCountingCircleRepo _doiCountingCirclesRepo;

    public DomainOfInfluenceCountingCircleInheritanceBuilder(DomainOfInfluenceCountingCircleRepo doiCountingCirclesRepo)
    {
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
    }

    /// <summary>
    /// Build the counting circle inheritance for a domain of influence and its parent.
    /// </summary>
    /// <param name="doiId">The domain of influence for which the assigned counting circles changes.</param>
    /// <param name="hierarchicalGreaterOrSelfDoiIds">A list of the <paramref name="doiId"/> and all IDs of its hierarchical parents.</param>
    /// <param name="countingCircleIdsToAdd">The counting circle IDs to add to the specified domain of influences.</param>
    /// <param name="countingCircleIdsToRemove">The counting circle IDs to remove from the specified domain of influences.</param>
    /// <param name="dateTime">The date time when the modification happened.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal async Task BuildInheritanceForCountingCircles(
        Guid doiId,
        List<Guid> hierarchicalGreaterOrSelfDoiIds,
        List<Guid> countingCircleIdsToAdd,
        List<Guid> countingCircleIdsToRemove,
        DateTime dateTime)
    {
        var existingEntries = await _doiCountingCirclesRepo.Query()
            .Where(doiCc => hierarchicalGreaterOrSelfDoiIds.Contains(doiCc.DomainOfInfluenceId) && countingCircleIdsToAdd.Contains(doiCc.CountingCircleId) && doiCc.SourceDomainOfInfluenceId == doiId)
            .ToListAsync();

        var newEntries = BuildDomainOfInfluenceCountingCircleEntries(doiId, hierarchicalGreaterOrSelfDoiIds, countingCircleIdsToAdd, existingEntries);

        await _doiCountingCirclesRepo.AddRange(newEntries, dateTime);
        await _doiCountingCirclesRepo.RemoveAll(hierarchicalGreaterOrSelfDoiIds, countingCircleIdsToRemove, dateTime, doiId);
    }

    private IEnumerable<DomainOfInfluenceCountingCircle> BuildDomainOfInfluenceCountingCircleEntries(
        Guid currentDoiId,
        IEnumerable<Guid> doiIds,
        IReadOnlyCollection<Guid> ccIds,
        IReadOnlyCollection<DomainOfInfluenceCountingCircle> existingEntries)
    {
        return doiIds.SelectMany(doiId =>
            ccIds.Where(ccId => !existingEntries.Any(x => x.CountingCircleId == ccId && x.DomainOfInfluenceId == doiId && x.SourceDomainOfInfluenceId == currentDoiId)).Select(ccId => new DomainOfInfluenceCountingCircle
            {
                CountingCircleId = ccId,
                DomainOfInfluenceId = doiId,
                SourceDomainOfInfluenceId = currentDoiId,
            }));
    }
}
