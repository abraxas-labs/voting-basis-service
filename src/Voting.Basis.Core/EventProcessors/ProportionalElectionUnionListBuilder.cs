// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class ProportionalElectionUnionListBuilder
{
    private readonly ProportionalElectionUnionListRepo _unionListRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEntry> _unionEntryRepo;
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;

    public ProportionalElectionUnionListBuilder(
        ProportionalElectionUnionListRepo unionListRepo,
        IDbRepository<DataContext, ProportionalElectionUnionEntry> unionEntryRepo,
        IDbRepository<DataContext, ProportionalElection> electionRepo)
    {
        _unionListRepo = unionListRepo;
        _unionEntryRepo = unionEntryRepo;
        _electionRepo = electionRepo;
    }

    public async Task RebuildLists(
        Guid unionId,
        List<Guid> proportionalElectionIds)
    {
        var proportionalElections = await _electionRepo
            .Query()
            .Where(p => proportionalElectionIds.Contains(p.Id))
            .Include(p => p.ProportionalElectionLists)
            .ToListAsync();

        var lists = proportionalElections
            .SelectMany(p => p.ProportionalElectionLists)
            .ToList();

        var listsByOrderNumberAndShortDescription = lists
            .GroupBy(l => new { l.OrderNumber, l.ShortDescription });

        var unionLists = listsByOrderNumberAndShortDescription
            .Select(l => new ProportionalElectionUnionList(
                unionId,
                l.Key.OrderNumber,
                l.Key.ShortDescription,
                l.ToList()))
            .ToList();

        await _unionListRepo.Replace(unionId, unionLists);
    }

    public async Task RebuildForProportionalElection(Guid proportionalElectionId)
    {
        var unionIds = await _unionEntryRepo.Query()
            .Where(e => e.ProportionalElectionId == proportionalElectionId)
            .Select(e => e.ProportionalElectionUnionId)
            .ToListAsync();

        await RebuildListsForUnions(unionIds);
    }

    public async Task RemoveListsWithNoEntries()
    {
        var listIdsWithNoEntries = await _unionListRepo.Query()
            .Include(l => l.ProportionalElectionUnionListEntries)
            .Where(l => l.ProportionalElectionUnionListEntries.Count == 0)
            .Select(x => x.Id)
            .ToListAsync();

        await _unionListRepo.DeleteRangeByKey(listIdsWithNoEntries);
    }

    private async Task RebuildListsForUnions(List<Guid> unionIds)
    {
        // complex groupby are not support in ef
        var electionIdsByUnion = (await _unionEntryRepo.Query()
            .Where(e => unionIds.Contains(e.ProportionalElectionUnionId))
            .ToListAsync())
            .GroupBy(e => e.ProportionalElectionUnionId, e => e.ProportionalElectionId)
            .ToList();

        foreach (var electionIds in electionIdsByUnion)
        {
            await RebuildLists(electionIds.Key, electionIds.ToList());
        }
    }
}
