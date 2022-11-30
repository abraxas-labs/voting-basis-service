// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class CountingCircleReader
{
    private readonly IDbRepository<DataContext, CountingCircle> _repo;
    private readonly DomainOfInfluencePermissionRepo _permissionRepo;
    private readonly DomainOfInfluenceCountingCircleRepo _doiCcRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly IDbRepository<DataContext, CountingCirclesMerger> _mergerRepo;
    private readonly IAuth _auth;

    public CountingCircleReader(
        IDbRepository<DataContext, CountingCircle> repo,
        DomainOfInfluencePermissionRepo permissionRepo,
        DomainOfInfluenceCountingCircleRepo doiCcRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        IDbRepository<DataContext, CountingCirclesMerger> mergerRepo,
        IAuth auth)
    {
        _repo = repo;
        _permissionRepo = permissionRepo;
        _doiCcRepo = doiCcRepo;
        _hierarchyRepo = hierarchyRepo;
        _mergerRepo = mergerRepo;
        _auth = auth;
    }

    public async Task<CountingCircle> Get(Guid id)
    {
        _auth.EnsureAdminOrElectionAdmin();
        return await BuildQuery()
                   .FirstOrDefaultAsync(x => x.Id == id)
               ?? throw new EntityNotFoundException(nameof(CountingCircle), id);
    }

    public async Task<List<CountingCircle>> List()
    {
        _auth.EnsureAdminOrElectionAdmin();
        return await BuildQuery()
            .OrderBy(cc => cc.Name)
            .ToListAsync();
    }

    public async Task<List<DomainOfInfluenceCountingCircle>> ListForDomainOfInfluence(Guid domainOfInfluenceId)
    {
        _auth.EnsureAdminOrElectionAdmin();

        var query = _doiCcRepo.Query()
            .Include(doiCc => doiCc.CountingCircle)
            .ThenInclude(cc => cc.ResponsibleAuthority)
            .Include(doiCc => doiCc.CountingCircle)
            .ThenInclude(cc => cc.ContactPersonDuringEvent)
            .Include(doiCc => doiCc.CountingCircle)
            .ThenInclude(cc => cc.ContactPersonAfterEvent)
            .Where(doiCc => doiCc.DomainOfInfluenceId == domainOfInfluenceId);

        if (!_auth.IsAdmin())
        {
            var doiPermission = await _permissionRepo.Query()
                .Where(p => p.DomainOfInfluenceId == domainOfInfluenceId && p.TenantId == _auth.Tenant.Id)
                .FirstOrDefaultAsync()
                ?? throw new EntityNotFoundException(domainOfInfluenceId);

            query = query.Where(doiCc => doiPermission.CountingCircleIds.Contains(doiCc.CountingCircleId));
        }

        return await query
            .OrderBy(doiCc => doiCc.CountingCircle.Name)
            .ToListAsync();
    }

    public async Task<List<CountingCircle>> GetAssignableListForDomainOfInfluence(Guid domainOfInfluenceId)
    {
        _auth.EnsureAdmin();

        var doiHierarchy = await _hierarchyRepo
            .Query()
            .FirstOrDefaultAsync(h => h.DomainOfInfluenceId == domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        var rootCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiCc.DomainOfInfluenceId == doiHierarchy.RootId)
            .Select(doiCc => doiCc.CountingCircleId)
            .ToListAsync();

        var selfNonInheritedCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiCc.DomainOfInfluenceId == domainOfInfluenceId && !doiCc.Inherited)
            .Select(doiCc => doiCc.CountingCircleId)
            .ToListAsync();

        return await _repo
            .Query()
            .Where(cc => !rootCcIds.Contains(cc.Id)
                || (rootCcIds.Contains(cc.Id) && selfNonInheritedCcIds.Contains(cc.Id)))
            .OrderBy(cc => cc.Name)
            .Include(cc => cc.DomainOfInfluences)
            .Include(cc => cc.ResponsibleAuthority)
            .Include(cc => cc.ContactPersonDuringEvent)
            .Include(cc => cc.ContactPersonAfterEvent)
            .ToListAsync();
    }

    public async Task<List<CountingCirclesMerger>> ListMergers(bool? merged)
    {
        _auth.EnsureAdmin();

        var query = _mergerRepo.Query();

        if (merged.HasValue)
        {
            query = query.Where(x => x.Merged == merged);
        }

        // ignore query filters to include already merged counting circles
        return await query
            .IgnoreQueryFilters()
            .Include(x => x.MergedCountingCircles.OrderBy(y => y.Name))
            .Include(x => x.NewCountingCircle!)
            .ThenInclude(x => x.ResponsibleAuthority)
            .OrderByDescending(x => x.ActiveFrom)
            .ToListAsync();
    }

    private IQueryable<CountingCircle> BuildQuery()
    {
        var query = _repo.Query();
        if (!_auth.IsAdmin())
        {
            // ef core does not support selectmany on array columns
            var doiPermissionCcIds = _permissionRepo.Query()
                .Where(p => p.TenantId == _auth.Tenant.Id)
                .Select(p => p.CountingCircleIds)
                .AsEnumerable()
                .SelectMany(c => c)
                .ToList();

            query = query.Where(cc =>
                doiPermissionCcIds.Contains(cc.Id)
                || cc.ResponsibleAuthority.SecureConnectId == _auth.Tenant.Id);
        }

        return query
            .Include(cc => cc.ResponsibleAuthority)
            .Include(cc => cc.ContactPersonDuringEvent)
            .Include(cc => cc.ContactPersonAfterEvent);
    }
}
