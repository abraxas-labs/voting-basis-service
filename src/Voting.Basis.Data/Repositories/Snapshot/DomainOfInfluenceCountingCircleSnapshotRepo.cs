// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories.Snapshot;

public class DomainOfInfluenceCountingCircleSnapshotRepo : DbRepository<DataContext, DomainOfInfluenceCountingCircleSnapshot>
{
    public DomainOfInfluenceCountingCircleSnapshotRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<Dictionary<Guid, List<DomainOfInfluenceCountingCircleSnapshot>>> CountingCirclesByDomainOfInfluenceId(
        DateTime dateTime,
        bool includeDeleted,
        List<Guid>? filteredCountingCircleIds = null,
        List<Guid>? filteredDomainOfInfluenceIds = null)
    {
        var query = Query();
        if (filteredCountingCircleIds != null)
        {
            query = query.Where(doiCc => filteredCountingCircleIds.Contains(doiCc.BasisCountingCircleId));
        }

        if (filteredDomainOfInfluenceIds != null)
        {
            query = query.Where(doiCc => filteredDomainOfInfluenceIds.Contains(doiCc.BasisDomainOfInfluenceId));
        }

        var entries = await query
            .ValidOn(dateTime)
            .Join(
                Context.CountingCircleSnapshots
                    .ValidOn(dateTime)
                    .Include(x => x.ResponsibleAuthority)
                    .Where(cc => includeDeleted || !cc.Deleted),
                doiCc => doiCc.BasisCountingCircleId,
                cc => cc.BasisId,
                (doiCc, cc) => new DomainOfInfluenceCountingCircleSnapshot
                {
                    Id = doiCc.Id,
                    BasisId = doiCc.BasisId,
                    BasisCountingCircleId = doiCc.BasisCountingCircleId,
                    BasisDomainOfInfluenceId = doiCc.BasisDomainOfInfluenceId,
                    Inherited = doiCc.Inherited,
                    CountingCircle = cc,
                    ValidFrom = doiCc.ValidFrom,
                    ValidTo = doiCc.ValidTo,
                    Deleted = doiCc.Deleted,
                })
            .OrderBy(c => c.CountingCircle.Name)
            .ToListAsync();

        // this group by is not yet supported by ef core, should be available with ef core 5
        return entries
            .GroupBy(x => x.BasisDomainOfInfluenceId)
            .ToDictionary(x => x.Key, x => x.ToList());
    }
}
