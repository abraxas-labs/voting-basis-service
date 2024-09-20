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
    public DomainOfInfluenceCountingCircleRepo(DataContext context, IMapper mapper)
        : base(context, mapper)
    {
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
            .AsTracking() // tracking is not needed but identity resolution, can be optimized with AsNoTrackingWithIdentityResolution
            .Include(c => c.CountingCircle)
            .ThenInclude(c => c.ResponsibleAuthority)
            .OrderBy(c => c.CountingCircle.Name)
            .ToListAsync();

        // this group by is not yet supported by ef core, should be available with ef core 5
        return entries
            .GroupBy(x => x.DomainOfInfluenceId)
            .ToDictionary(x => x.Key, x => x.ToList());
    }

    public async Task<List<Guid>> GetCountingCircleGuidsByDomainOfInfluenceId(Guid domainOfInfluenceId)
    {
        return await Query()
            .Where(c => c.DomainOfInfluenceId == domainOfInfluenceId)
            .Select(c => c.CountingCircleId)
            .ToListAsync();
    }

    /// <summary>
    /// Removes all Entries, where any of the DomainOfInfluenceIds matches any of the CountingCircleIds.
    /// </summary>
    /// <param name="domainOfInfluenceIds">DomainofInfluenceIds to delete.</param>
    /// <param name="countingCircleIds">CountingCircleIds to delete.</param>
    /// <param name="dateTime">Timestamp for Snapshots.</param>
    /// <returns>A Task.</returns>
    public async Task RemoveAll(List<Guid> domainOfInfluenceIds, List<Guid> countingCircleIds, DateTime dateTime)
    {
        if (domainOfInfluenceIds.Count == 0 || countingCircleIds.Count == 0)
        {
            return;
        }

        var existingEntries = await Query()
            .Where(doiCc => domainOfInfluenceIds.Contains(doiCc.DomainOfInfluenceId) && countingCircleIds.Contains(doiCc.CountingCircleId))
            .ToListAsync();

        await DeleteRange(existingEntries, dateTime);
    }
}
