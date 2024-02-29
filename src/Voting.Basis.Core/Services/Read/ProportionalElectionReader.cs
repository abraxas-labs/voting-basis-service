// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class ProportionalElectionReader : PoliticalBusinessReader<ProportionalElection>
{
    private readonly IDbRepository<DataContext, ProportionalElectionList> _listRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionListUnion> _listUnionRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidate> _candidateRepo;

    public ProportionalElectionReader(
        IDbRepository<DataContext, ProportionalElection> repo,
        IDbRepository<DataContext, ProportionalElectionList> listRepo,
        IDbRepository<DataContext, ProportionalElectionListUnion> listUnionRepo,
        IDbRepository<DataContext, ProportionalElectionCandidate> candidateRepo,
        IAuth auth,
        PermissionService permissionService)
        : base(auth, permissionService, repo)
    {
        _listRepo = listRepo;
        _listUnionRepo = listUnionRepo;
        _candidateRepo = candidateRepo;
    }

    public async Task<IEnumerable<ProportionalElectionList>> GetLists(Guid electionId)
    {
        var proportionalElection = await Repo.Query()
            .Include(p => p.ProportionalElectionLists)
            .Include(p => p.DomainOfInfluence)
            .FirstOrDefaultAsync(p => p.Id == electionId)
            ?? throw new EntityNotFoundException(electionId);

        await EnsureAllowedToRead(proportionalElection.DomainOfInfluenceId);
        return proportionalElection.ProportionalElectionLists.OrderBy(l => l.Position);
    }

    public async Task<ProportionalElectionList> GetList(Guid listId)
    {
        var list = await _listRepo.Query()
            .Include(l => l.ProportionalElection)
            .FirstOrDefaultAsync(l => l.Id == listId)
            ?? throw new EntityNotFoundException(listId);

        await EnsureAllowedToRead(list.ProportionalElection.DomainOfInfluenceId);
        return list;
    }

    public async Task<IEnumerable<ProportionalElectionListUnion>> GetListUnions(Guid electionId)
    {
        var proportionalElection = await Repo.Query()
            .FirstOrDefaultAsync(p => p.Id == electionId)
            ?? throw new EntityNotFoundException(electionId);

        await EnsureAllowedToRead(proportionalElection.DomainOfInfluenceId);

        var listUnions = await _listUnionRepo.Query()
            .AsSplitQuery()
            .Where(lu => lu.ProportionalElectionId == electionId && lu.ProportionalElectionRootListUnionId == null)
            .Include(lu => lu.ProportionalElectionSubListUnions)
            .ThenInclude(c => c.ProportionalElectionListUnionEntries)
            .Include(lu => lu.ProportionalElectionListUnionEntries)
            .OrderBy(lu => lu.Position)
            .ToListAsync();

        foreach (var listUnion in listUnions)
        {
            OrderListUnion(listUnion);
        }

        return listUnions;
    }

    public async Task<ProportionalElectionListUnion> GetListUnion(Guid listUnionId)
    {
        var listUnion = await _listUnionRepo.Query()
            .AsSplitQuery()
            .Include(lu => lu.ProportionalElectionSubListUnions)
            .ThenInclude(c => c.ProportionalElectionListUnionEntries)
            .Include(lu => lu.ProportionalElectionListUnionEntries)
            .Include(l => l.ProportionalElection)
            .FirstOrDefaultAsync(l => l.Id == listUnionId)
            ?? throw new EntityNotFoundException(listUnionId);

        OrderListUnion(listUnion);

        await EnsureAllowedToRead(listUnion.ProportionalElection.DomainOfInfluenceId);
        return listUnion;
    }

    public async Task<IEnumerable<ProportionalElectionCandidate>> GetCandidates(Guid listId)
    {
        var list = await _listRepo.Query()
            .IgnoreQueryFilters() // candidate could contain a soft deleted party, whose properties should be displayed.
            .Include(l => l.ProportionalElection)
            .Include(l => l.ProportionalElectionCandidates)
                .ThenInclude(c => c.Party)
            .FirstOrDefaultAsync(l => l.Id == listId)
            ?? throw new EntityNotFoundException(listId);

        await EnsureAllowedToRead(list.ProportionalElection.DomainOfInfluenceId);
        return list.ProportionalElectionCandidates.OrderBy(c => c.Position);
    }

    public async Task<ProportionalElectionCandidate> GetCandidate(Guid candidateId)
    {
        var candidate = await _candidateRepo.Query()
            .IgnoreQueryFilters() // candidate could contain a soft deleted party, whose properties should be displayed.
            .Include(c => c.ProportionalElectionList)
            .ThenInclude(l => l.ProportionalElection)
            .Include(c => c.Party)
            .FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new EntityNotFoundException(candidateId);

        await EnsureAllowedToRead(candidate.ProportionalElectionList.ProportionalElection.DomainOfInfluenceId);
        return candidate;
    }

    protected override async Task<ProportionalElection> QueryById(Guid id)
    {
        return await Repo.Query()
                    .Include(v => v.DomainOfInfluence)
                    .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task EnsureAllowedToRead(Guid proportionalElectionDomainOfInfluenceId)
    {
        await PermissionService.EnsureIsOwnerOfDomainOfInfluence(proportionalElectionDomainOfInfluenceId);
    }

    private void OrderListUnion(ProportionalElectionListUnion listUnion)
    {
        listUnion.ProportionalElectionSubListUnions = listUnion.ProportionalElectionSubListUnions.OrderBy(c => c.Position).ToList();
        listUnion.ProportionalElectionListUnionEntries = listUnion.ProportionalElectionListUnionEntries.OrderBy(x => x.ProportionalElectionListId).ToList();

        foreach (var subListUnion in listUnion.ProportionalElectionSubListUnions)
        {
            subListUnion.ProportionalElectionListUnionEntries = subListUnion.ProportionalElectionListUnionEntries.OrderBy(x => x.ProportionalElectionListId).ToList();
        }
    }
}
