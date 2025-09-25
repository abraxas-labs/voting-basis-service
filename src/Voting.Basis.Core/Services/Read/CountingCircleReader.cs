// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data;
using Voting.Basis.Data.Extensions;
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
    private readonly CantonSettingsRepo _cantonSettingsRepo;
    private readonly IAuth _auth;

    public CountingCircleReader(
        IDbRepository<DataContext, CountingCircle> repo,
        DomainOfInfluencePermissionRepo permissionRepo,
        DomainOfInfluenceCountingCircleRepo doiCcRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        IDbRepository<DataContext, CountingCirclesMerger> mergerRepo,
        CantonSettingsRepo cantonSettingsRepo,
        IAuth auth)
    {
        _repo = repo;
        _permissionRepo = permissionRepo;
        _doiCcRepo = doiCcRepo;
        _hierarchyRepo = hierarchyRepo;
        _mergerRepo = mergerRepo;
        _cantonSettingsRepo = cantonSettingsRepo;
        _auth = auth;
    }

    public async Task<CountingCircle> Get(Guid id)
    {
        var query = await BuildQuery();
        return await query
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), id);
    }

    public async Task<List<CountingCircle>> List()
    {
        var query = await BuildQuery();
        return await query
            .OrderBy(cc => cc.Name)
            .ToListAsync();
    }

    public async Task<List<DomainOfInfluenceCountingCircle>> ListForDomainOfInfluence(Guid domainOfInfluenceId)
    {
        var query = _doiCcRepo.Query()
            .Include(doiCc => doiCc.CountingCircle)
            .ThenInclude(cc => cc.ResponsibleAuthority)
            .Include(doiCc => doiCc.CountingCircle)
            .ThenInclude(cc => cc.ContactPersonDuringEvent)
            .Include(doiCc => doiCc.CountingCircle)
            .ThenInclude(cc => cc.ContactPersonAfterEvent)
            .Where(doiCc => doiCc.DomainOfInfluenceId == domainOfInfluenceId);

        if (_auth.HasPermission(Permissions.CountingCircle.ReadAll))
        {
            // No restrictions
        }
        else if (_auth.HasPermission(Permissions.CountingCircle.ReadSameCanton))
        {
            var cantons = await GetAccessibleCantons();
            query = query.Where(doiCc => cantons.Contains(doiCc.CountingCircle.Canton));
        }
        else if (_auth.HasPermission(Permissions.CountingCircle.Read))
        {
            var doiPermission = await _permissionRepo.Query()
                .Where(p => p.DomainOfInfluenceId == domainOfInfluenceId && p.TenantId == _auth.Tenant.Id)
                .FirstOrDefaultAsync()
                ?? throw new EntityNotFoundException(domainOfInfluenceId);

            query = query.Where(doiCc => doiPermission.CountingCircleIds.Contains(doiCc.CountingCircleId));
        }

        var doiCcs = await query
            .OrderBy(doiCc => doiCc.CountingCircle.Name)
            .ToListAsync();

        return doiCcs.DistinctBy(x => new { x.CountingCircleId, x.DomainOfInfluenceId }).ToList();
    }

    public async Task<List<CountingCircle>> GetAssignableListForDomainOfInfluence(Guid domainOfInfluenceId)
    {
        var doiHierarchy = await _hierarchyRepo
            .Query()
            .FirstOrDefaultAsync(h => h.DomainOfInfluenceId == domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        var hierarchicalGreaterNonInheritedCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiHierarchy.ParentIds.Contains(doiCc.DomainOfInfluenceId))
            .WhereIsNotInherited()
            .Select(doiCc => doiCc.CountingCircleId)
            .Distinct()
            .ToListAsync();

        var inheritedCcIds = await _doiCcRepo
            .Query()
            .Where(doiCc => doiCc.DomainOfInfluenceId == domainOfInfluenceId)
            .WhereIsInherited()
            .Select(doiCc => doiCc.CountingCircleId)
            .Distinct()
            .ToListAsync();

        var query = _repo.Query();

        if (_auth.HasPermission(Permissions.CountingCircle.ReadSameCanton))
        {
            var cantons = await GetAccessibleCantons();
            query = query.Where(cc => cantons.Contains(cc.Canton));
        }

        return await query
            .Where(cc => !inheritedCcIds.Contains(cc.Id) && !hierarchicalGreaterNonInheritedCcIds.Contains(cc.Id))
            .OrderBy(cc => cc.Name)
            .Include(cc => cc.DomainOfInfluences)
            .Include(cc => cc.ResponsibleAuthority)
            .Include(cc => cc.ContactPersonDuringEvent)
            .Include(cc => cc.ContactPersonAfterEvent)
            .ToListAsync();
    }

    public async Task<List<CountingCirclesMerger>> ListMergers(bool? merged)
    {
        var query = _mergerRepo.Query();

        if (merged.HasValue)
        {
            query = query.Where(x => x.Merged == merged);
        }

        if (_auth.HasPermission(Permissions.CountingCircle.ReadSameCanton) && !_auth.HasPermission(Permissions.CountingCircle.ReadAll))
        {
            var cantons = await GetAccessibleCantons();
            query = query.Where(x => cantons.Contains(x.NewCountingCircle!.Canton));
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

    internal async Task<bool> OwnsAnyECountingCountingCircle()
    {
        var tenantId = _auth.Tenant.Id;

        return await _repo.Query()
            .Where(cc => cc.ECounting && cc.ResponsibleAuthority.SecureConnectId == tenantId)
            .AnyAsync();
    }

    private async Task<IQueryable<CountingCircle>> BuildQuery()
    {
        var query = _repo.Query();

        if (_auth.HasPermission(Permissions.CountingCircle.ReadAll))
        {
            // No restrictions
        }
        else if (_auth.HasPermission(Permissions.CountingCircle.ReadSameCanton))
        {
            var cantons = await GetAccessibleCantons();
            query = query.Where(cc => cantons.Contains(cc.Canton));
        }
        else if (_auth.HasPermission(Permissions.CountingCircle.Read))
        {
            // EF Core does not support SelectMany on array columns, do that step locally
            var doiPermissionCcIdsList = await _permissionRepo.Query()
                .Where(p => p.TenantId == _auth.Tenant.Id)
                .Select(p => p.CountingCircleIds)
                .ToListAsync();
            var doiPermissionCcIds = doiPermissionCcIdsList
                .SelectMany(x => x)
                .ToHashSet();

            query = query.Where(cc =>
                doiPermissionCcIds.Contains(cc.Id)
                || cc.ResponsibleAuthority.SecureConnectId == _auth.Tenant.Id);
        }

        return query
            .Include(cc => cc.ResponsibleAuthority)
            .Include(cc => cc.ContactPersonDuringEvent)
            .Include(cc => cc.ContactPersonAfterEvent)
            .Include(cc => cc.Electorates.OrderBy(e => e.DomainOfInfluenceTypes[0]));
    }

    private Task<List<DomainOfInfluenceCanton>> GetAccessibleCantons()
    {
        return _cantonSettingsRepo.Query()
            .Where(x => x.SecureConnectId == _auth.Tenant.Id)
            .Select(x => x.Canton)
            .ToListAsync();
    }
}
