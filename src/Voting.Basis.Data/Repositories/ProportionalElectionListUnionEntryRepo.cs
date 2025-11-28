// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class ProportionalElectionListUnionEntryRepo : DbRepository<DataContext, ProportionalElectionListUnionEntry>
{
    public ProportionalElectionListUnionEntryRepo(DataContext context)
        : base(context)
    {
    }

    public async Task Replace(Guid listUnionId, IEnumerable<ProportionalElectionListUnionEntry> entries)
    {
        var existingEntries = await Set.Where(e => e.ProportionalElectionListUnionId == listUnionId).ToArrayAsync();

        Set.RemoveRange(existingEntries);
        Set.AddRange(entries);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// All ListUnionEntries in the SubListUnions which aren't in the ListIds of the RootListUnion, will be deleted.
    /// </summary>
    /// <param name="subListUnionIds">All SubListUnion Ids of the specific RootListUnion.</param>
    /// <param name="rootListIds">All List Ids of the specific RootListUnion.</param>
    /// <returns>A task.</returns>
    public async Task DeleteSubListUnionListEntriesByRootListIds(List<Guid> subListUnionIds, List<Guid> rootListIds)
    {
        var subListUnionEntries = Set.Where(e => subListUnionIds.Contains(e.ProportionalElectionListUnionId));
        await subListUnionEntries.Where(e => !rootListIds.Contains(e.ProportionalElectionListId)).ExecuteDeleteAsync();
        await Context.SaveChangesAsync();
    }
}
