// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Basis.Data.Repositories.Snapshot;

namespace Voting.Basis.Data.Repositories;

public class DomainOfInfluenceCountingCircleRepo
    : HasSnapshotDbRepository<DomainOfInfluenceCountingCircle, DomainOfInfluenceCountingCircleSnapshot>
{
    private readonly DomainOfInfluenceRepo _doiRepo;

    public DomainOfInfluenceCountingCircleRepo(DataContext context, IMapper mapper, DomainOfInfluenceRepo doiRepo)
        : base(context, mapper)
    {
        _doiRepo = doiRepo;
    }

    public async Task<Dictionary<Guid, List<DomainOfInfluenceCountingCircle>>> CountingCirclesByDomainOfInfluenceId(
        List<Guid>? filteredCountingCircleIds = null,
        List<Guid>? filteredDomainOfInfluenceIds = null)
    {
        var query = Query();
        if (filteredCountingCircleIds != null)
        {
            query = query.Where(c => filteredCountingCircleIds.Contains(c.CountingCircleId));
        }

        if (filteredDomainOfInfluenceIds != null)
        {
            query = query.Where(c => filteredDomainOfInfluenceIds.Contains(c.DomainOfInfluenceId));
        }

        var entries = await query
            .Include(c => c.CountingCircle)
            .ThenInclude(c => c.ResponsibleAuthority)
            .OrderBy(c => c.CountingCircle.Name)
            .ToListAsync();

        return entries
            .GroupBy(x => x.DomainOfInfluenceId)
            .ToDictionary(x => x.Key, x => x.ToList());
    }

    /// <summary>
    /// Removes all Entries, where any of the DomainOfInfluenceIds matches any of the CountingCircleIds.
    /// </summary>
    /// <param name="domainOfInfluenceIds">DomainOfInfluenceIds to delete.</param>
    /// <param name="countingCircleIds">CountingCircleIds to delete.</param>
    /// <param name="dateTime">Timestamp for Snapshots.</param>
    /// <param name="currentDoiId">Current domain of influence id which is responsible for the deletion.</param>
    /// <returns>A Task.</returns>
    public async Task RemoveAll(List<Guid> domainOfInfluenceIds, List<Guid> countingCircleIds, DateTime dateTime, Guid currentDoiId)
    {
        if (domainOfInfluenceIds.Count == 0 || countingCircleIds.Count == 0)
        {
            return;
        }

        var hierarchicalLowerOrSelfDoiIds = await _doiRepo.GetHierarchicalLowerOrSelfDomainOfInfluenceIds(currentDoiId);

        var existingEntries = await Query()
            .Where(doiCc => domainOfInfluenceIds.Contains(doiCc.DomainOfInfluenceId) &&
                            countingCircleIds.Contains(doiCc.CountingCircleId) &&
                            hierarchicalLowerOrSelfDoiIds.Contains(doiCc.SourceDomainOfInfluenceId))
            .ToListAsync();

        await DeleteRange(existingEntries, dateTime);
    }
}
