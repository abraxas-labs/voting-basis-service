// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Services.Read;

public class ProportionalElectionUnionReader
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _repo;
    private readonly PermissionService _permissionService;

    public ProportionalElectionUnionReader(
        IDbRepository<DataContext, ProportionalElectionUnion> repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public async Task<List<ElectionCandidate>> GetCandidates(Guid id)
    {
        var union = await _repo.Query()
            .AsSplitQuery()
            .Include(u => u.Contest)
            .Include(u => u.ProportionalElectionUnionEntries)
            .ThenInclude(ue => ue.ProportionalElection.ProportionalElectionLists)
            .ThenInclude(p => p.ProportionalElectionCandidates)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new EntityNotFoundException(id);

        await _permissionService.EnsureCanReadContest(union.Contest);

        var proportionalElectionUnionCandidates = union.ProportionalElectionUnionEntries
            .SelectMany(pue => pue.ProportionalElection.ProportionalElectionLists)
            .SelectMany(pl => pl.ProportionalElectionCandidates)
            .ToList();

        foreach (var proportionalElectionUnionCandidate in proportionalElectionUnionCandidates)
        {
            proportionalElectionUnionCandidate.Number =
                $"{proportionalElectionUnionCandidate.ProportionalElectionList.OrderNumber}.{proportionalElectionUnionCandidate.Number}";
        }

        var candidates = proportionalElectionUnionCandidates.Cast<ElectionCandidate>().ToList();
        return candidates.OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToList();
    }

    public async Task<List<PoliticalBusiness>> GetPoliticalBusinesses(Guid id)
    {
        var union = await _repo.Query()
            .Include(u => u.Contest)
            .Include(u => u.ProportionalElectionUnionEntries)
            .ThenInclude(ue => ue.ProportionalElection.DomainOfInfluence)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new EntityNotFoundException(id);

        await _permissionService.EnsureCanReadContest(union.Contest);
        return union.ProportionalElectionUnionEntries.Select(ue => ue.ProportionalElection)
            .OrderBy(p => p.PoliticalBusinessNumber)
            .Cast<PoliticalBusiness>()
            .ToList();
    }

    public async Task<List<ProportionalElectionUnionList>> GetUnionLists(Guid id)
    {
        var union = await _repo.Query()
            .AsSplitQuery()
            .Include(u => u.Contest)
            .Include(pu => pu.ProportionalElectionUnionLists)
            .ThenInclude(ul => ul.ProportionalElectionUnionListEntries)
            .ThenInclude(e => e.ProportionalElectionList.ProportionalElection)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new EntityNotFoundException(id);

        await _permissionService.EnsureCanReadContest(union.Contest);

        return union.ProportionalElectionUnionLists
            .OrderBy(l => l.OrderNumber)
            .ThenBy(l => l.PoliticalBusinessNumbers)
            .ToList();
    }
}
