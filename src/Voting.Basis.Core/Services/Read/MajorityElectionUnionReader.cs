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

public class MajorityElectionUnionReader
{
    private readonly IDbRepository<DataContext, MajorityElectionUnion> _repo;
    private readonly PermissionService _permissionService;

    public MajorityElectionUnionReader(
        IDbRepository<DataContext, MajorityElectionUnion> repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public async Task<List<ElectionCandidate>> GetCandidates(Guid id)
    {
        var majorityElectionUnion = await BuildQuery()
            .AsSplitQuery()
            .Include(u => u.Contest)
            .Include(u => u.MajorityElectionUnionEntries)
            .ThenInclude(ue => ue.MajorityElection.MajorityElectionCandidates)
            .Include(u => u.MajorityElectionUnionEntries)
            .ThenInclude(ue => ue.MajorityElection.SecondaryMajorityElections)
            .ThenInclude(sme => sme.Candidates)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new EntityNotFoundException(id);

        await _permissionService.EnsureCanReadContest(majorityElectionUnion.Contest);

        var candidates = new List<ElectionCandidate>();
        var majorityElectionUnionEntries = majorityElectionUnion.MajorityElectionUnionEntries;

        candidates.AddRange(
            majorityElectionUnionEntries
                .SelectMany(mue => mue.MajorityElection.MajorityElectionCandidates));

        candidates.AddRange(
            majorityElectionUnionEntries
                .SelectMany(mue => mue.MajorityElection.SecondaryMajorityElections)
                .SelectMany(sme => sme.Candidates)
                .Where(c => c.CandidateReferenceId == null));

        return candidates.OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToList();
    }

    public async Task<List<PoliticalBusiness>> GetPoliticalBusinesses(Guid id)
    {
        var union = await BuildQuery()
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new EntityNotFoundException(id);

        await _permissionService.EnsureCanReadContest(union.Contest);

        return union.MajorityElectionUnionEntries
            .Select(ue => ue.MajorityElection)
            .OrderBy(m => m.PoliticalBusinessNumber)
            .Cast<PoliticalBusiness>()
            .ToList();
    }

    private IQueryable<MajorityElectionUnion> BuildQuery()
    {
        return _repo.Query()
            .Include(u => u.Contest)
            .Include(u => u.MajorityElectionUnionEntries)
            .ThenInclude(ue => ue.MajorityElection.DomainOfInfluence);
    }
}
