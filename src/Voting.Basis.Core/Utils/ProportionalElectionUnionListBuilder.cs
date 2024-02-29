// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Utils;

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

    public Task RebuildLists(Guid unionId, List<Guid> electionIds) =>
        RebuildLists(new() { { unionId, electionIds } });

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

    internal List<ProportionalElectionUnionList> BuildUnionLists(Guid unionId, List<ProportionalElectionList> lists)
    {
        var listsByOrderNumberAndGermanShortDescription = lists
            .Where(l => l.ShortDescription.ContainsKey(Languages.German))
            .GroupBy(l => new { l.OrderNumber, GermanShortDescription = l.ShortDescription[Languages.German] });

        return listsByOrderNumberAndGermanShortDescription
            .Select(l => new ProportionalElectionUnionList(
                unionId,
                l.Key.OrderNumber,
                l.OrderBy(i => i.Id).First().ShortDescription,
                l.ToList()))
            .ToList();
    }

    private async Task RebuildListsForUnions(List<Guid> unionIds)
    {
        // complex groupby are not support in ef
        var electionIdsByUnion = (await _unionEntryRepo.Query()
            .Where(e => unionIds.Contains(e.ProportionalElectionUnionId))
            .ToListAsync())
            .GroupBy(e => e.ProportionalElectionUnionId, e => e.ProportionalElectionId)
            .ToDictionary(e => e.Key, e => e.ToList());

        await RebuildLists(electionIdsByUnion);
    }

    private async Task RebuildLists(Dictionary<Guid, List<Guid>> electionIdsByUnion)
    {
        var proportionalElectionIds = electionIdsByUnion.Values.SelectMany(e => e).ToHashSet();

        var proportionalElections = await _electionRepo
            .Query()
            .Where(p => proportionalElectionIds.Contains(p.Id))
            .Include(p => p.ProportionalElectionLists)
            .ToListAsync();

        var unionLists = new List<ProportionalElectionUnionList>();
        foreach (var (unionId, electionIds) in electionIdsByUnion)
        {
            var lists = proportionalElections.Where(p => electionIds.Contains(p.Id))
                .SelectMany(p => p.ProportionalElectionLists)
                .ToList();

            unionLists.AddRange(BuildUnionLists(unionId, lists));
        }

        await _unionListRepo.Replace(electionIdsByUnion.Keys.ToList(), unionLists);
    }
}
