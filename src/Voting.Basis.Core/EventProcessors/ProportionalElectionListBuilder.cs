// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class ProportionalElectionListBuilder
{
    private readonly ProportionalElectionListRepo _repo;

    public ProportionalElectionListBuilder(ProportionalElectionListRepo repo)
    {
        _repo = repo;
    }

    internal Task UpdateListUnionDescriptionsReferencingList(Guid listId)
    {
        return UpdateListUnionDescriptions(q => q
            .Where(list => list.ProportionalElectionListUnionEntries
                .Any(listUnionEntry => listUnionEntry.ProportionalElectionListUnion.ProportionalElectionListUnionEntries
                    .Any(nestedListUnionEntry => nestedListUnionEntry.ProportionalElectionListId == listId))));
    }

    internal Task UpdateListUnionDescriptions(IEnumerable<Guid> listIds)
    {
        return UpdateListUnionDescriptions(q => q
            .Where(list => listIds.Contains(list.Id)));
    }

    private async Task UpdateListUnionDescriptions(Func<IQueryable<ProportionalElectionList>, IQueryable<ProportionalElectionList>> queryBuilder)
    {
        var lists = await queryBuilder(_repo.Query())
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.ProportionalElectionListUnionEntries)
            .ThenInclude(x => x.ProportionalElectionListUnion.ProportionalElectionListUnionEntries)
            .ThenInclude(x => x.ProportionalElectionList)
            .ToListAsync();

        foreach (var list in lists)
        {
            list.ListUnionDescription = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(list, false);
            list.SubListUnionDescription = ProportionalListUnionDescriptionBuilder.BuildListUnionDescription(list, true);
        }

        await _repo.UpdateRangeIgnoreRelations(lists);
    }
}
