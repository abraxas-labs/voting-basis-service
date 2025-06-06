﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Models;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Permission;

public class PermissionService
{
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly DomainOfInfluencePermissionRepo _permissionRepo;
    private readonly CantonSettingsRepo _cantonSettingsRepo;
    private readonly IAuthStore _authStore;
    private readonly AppConfig _config;
    private readonly IAuth _auth;

    public PermissionService(
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        DomainOfInfluencePermissionRepo permissionRepo,
        CantonSettingsRepo cantonSettingsRepo,
        IAuth auth,
        IAuthStore authStore,
        AppConfig config)
    {
        _doiRepo = doiRepo;
        _contestRepo = contestRepo;
        _hierarchyRepo = hierarchyRepo;
        _permissionRepo = permissionRepo;
        _cantonSettingsRepo = cantonSettingsRepo;
        _auth = auth;
        _authStore = authStore;
        _config = config;
    }

    /// <summary>
    /// Ensures that the current user is the owner (user's tenant matches the domain of influence responsible tenant) of the specified domain of influence.
    /// </summary>
    /// <param name="domainOfInfluenceId">The ID of the domain of influence to check.</param>
    /// <exception cref="ValidationException">Thrown if the user is not the owner of the domain of influence.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureIsOwnerOfDomainOfInfluence(Guid domainOfInfluenceId)
    {
        var tenantId = _auth.Tenant.Id;
        var doiBelongsToThisTenant = await _doiRepo.Query()
            .IgnoreQueryFilters() // Deleted DOI should still work
            .AnyAsync(doi => doi.Id == domainOfInfluenceId && doi.SecureConnectId == tenantId);

        if (!doiBelongsToThisTenant)
        {
            throw new ValidationException("Invalid domain of influence, does not belong to this tenant");
        }
    }

    public async Task EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(Guid domainOfInfluenceId, bool isReadAccess)
    {
        var tenantId = _auth.Tenant.Id;

        var sameCantonPermission = isReadAccess
            ? Permissions.PoliticalBusiness.ReadActionsTenantSameCanton
            : Permissions.PoliticalBusiness.WriteActionsTenantSameCanton;
        if (_auth.HasPermission(sameCantonPermission))
        {
            var doi = await _doiRepo.Query()
                .IgnoreQueryFilters() // Deleted DOI should still work
                .FirstOrDefaultAsync(doi => doi.Id == domainOfInfluenceId);

            var doiBelongsToCanton = doi != null && await _cantonSettingsRepo.Query()
                .AnyAsync(c => c.SecureConnectId == tenantId && doi.Canton == c.Canton);

            if (!doiBelongsToCanton)
            {
                throw new ForbiddenException("Invalid domain of influence, does not belong to any owning canton");
            }

            return;
        }

        var sameTenantPermission = isReadAccess
            ? Permissions.PoliticalBusiness.ReadActionsSameTenant
            : Permissions.PoliticalBusiness.WriteActionsSameTenant;
        if (!_auth.HasPermission(sameTenantPermission))
        {
            throw new ForbiddenException("Not enough permissions to perform this action");
        }

        var doiBelongsToThisTenant = await _doiRepo.Query()
            .IgnoreQueryFilters() // Deleted DOI should still work
            .AnyAsync(doi => doi.Id == domainOfInfluenceId && doi.SecureConnectId == tenantId);

        if (!doiBelongsToThisTenant)
        {
            throw new ForbiddenException("Invalid domain of influence, does not belong to this tenant");
        }
    }

    public async Task EnsureIsOwnerOfDomainOfInfluencesOrHasAdminPermissions(IEnumerable<Guid> domainOfInfluenceIds, bool isReadAccess)
    {
        var tenantId = _auth.Tenant.Id;
        var doiIds = domainOfInfluenceIds.ToHashSet();

        var sameCantonPermission = isReadAccess
            ? Permissions.PoliticalBusiness.ReadActionsTenantSameCanton
            : Permissions.PoliticalBusiness.WriteActionsTenantSameCanton;
        if (_auth.HasPermission(sameCantonPermission))
        {
            var authorizedCantons = await _cantonSettingsRepo.Query()
                .Where(c => c.SecureConnectId == tenantId)
                .Select(c => c.Canton)
                .ToListAsync();

            var cantons = await _doiRepo.Query()
                .IgnoreQueryFilters() // Deleted DOI should still work
                .Where(doi => doiIds.Contains(doi.Id))
                .Select(doi => doi.Canton)
                .ToListAsync();

            var doisBelongsToCanton = cantons.Count > 0 && cantons.All(authorizedCantons.Contains);
            if (!doisBelongsToCanton)
            {
                throw new ForbiddenException("Invalid domain of influence, does not belong to any owning canton");
            }

            return;
        }

        var doiIdsOfThisTenant = await _doiRepo.Query()
            .IgnoreQueryFilters() // Deleted DOI should still work
            .Where(doi => doi.SecureConnectId == tenantId)
            .Select(doi => doi.Id)
            .ToListAsync();

        doiIds.ExceptWith(doiIdsOfThisTenant);

        if (doiIds.FirstOrDefault() is var doiId && doiId != default)
        {
            throw new ForbiddenException("Invalid domain of influence, does not belong to this tenant");
        }
    }

    /// <summary>
    /// Ensures that a list of domain of influences are children of a domain of influence or the domain of influence itself.
    /// </summary>
    /// <param name="parentDomainOfInfluenceId">The ID of the domain of influence.</param>
    /// <param name="childrenDomainOfInfluenceIds">The list of domain of influence IDs, which should be children of the <paramref name="parentDomainOfInfluenceId"/> or the same ID.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the domain of influence of <paramref name="parentDomainOfInfluenceId"/> cannot be found.</exception>
    /// <exception cref="ValidationException">Thrown if the <paramref name="childrenDomainOfInfluenceIds"/> are not all children or the same ID.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureDomainOfInfluencesAreChildrenOrSelf(Guid parentDomainOfInfluenceId, params Guid[] childrenDomainOfInfluenceIds)
    {
        var baseNode = await _hierarchyRepo.Query()
            .FirstOrDefaultAsync(h => h.DomainOfInfluenceId == parentDomainOfInfluenceId)
            ?? throw new EntityNotFoundException(parentDomainOfInfluenceId);

        var allDoisAreChildrenOrSelf = childrenDomainOfInfluenceIds.All(childId =>
            childId == parentDomainOfInfluenceId || baseNode.ChildIds.Any(n => n == childId));

        if (!allDoisAreChildrenOrSelf)
        {
            throw new ValidationException("Invalid domain of influence(s), some ids are not children of the parent node");
        }
    }

    public async Task EnsureCanReadContest(Guid contestId)
    {
        var contest = await _contestRepo.GetByKey(contestId)
            ?? throw new EntityNotFoundException(contestId);

        await EnsureCanReadContest(contest);
    }

    public async Task EnsureCanReadContest(Contest contest)
    {
        await EnsureCanReadDomainOfInfluence(contest.DomainOfInfluenceId);
    }

    public async Task EnsureCanReadDomainOfInfluence(DomainOfInfluence domainOfInfluence)
    {
        if (!await CanAccessDomainOfInfluence(domainOfInfluence))
        {
            throw new ForbiddenException($"you have no read access on domain of influence {domainOfInfluence.Id}");
        }
    }

    public async Task EnsureCanReadDomainOfInfluence(Guid domainOfInfluenceId)
    {
        var doi = await _doiRepo.Query()
            .IgnoreQueryFilters() // Deleted DOI should still work
            .FirstOrDefaultAsync(x => x.Id == domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);
        if (!await CanAccessDomainOfInfluence(doi))
        {
            throw new ForbiddenException($"you have no read access on domain of influence {domainOfInfluenceId}");
        }
    }

    public async Task<AccessibleDomainOfInfluenceHierarchyGroups> GetAccessibleDomainOfInfluenceHierarchyGroups()
    {
        var tenantId = _auth.Tenant.Id;
        var tenantDoiIds = await _doiRepo.Query()
            .Where(doi => doi.SecureConnectId == tenantId)
            .Select(doi => doi.Id)
            .ToHashSetAsync();

        var parentDoiIds = await _permissionRepo.Query()
            .Where(p => p.TenantId == tenantId && p.IsParent)
            .Select(p => p.DomainOfInfluenceId)
            .ToHashSetAsync();

        var childDoiIds = (await _hierarchyRepo.Query()
            .Where(doi => doi.TenantId == tenantId)
            .Select(doi => doi.ChildIds)
            .Distinct()
            .ToListAsync())
            .SelectMany(doi => doi)
            .ToHashSet();

        return new AccessibleDomainOfInfluenceHierarchyGroups(tenantDoiIds, parentDoiIds, childDoiIds);
    }

    internal Task<bool> IsOwnerOfCanton(DomainOfInfluenceCanton canton)
    {
        return _cantonSettingsRepo.Query()
            .AnyAsync(x => x.Canton == canton && x.SecureConnectId == _auth.Tenant.Id);
    }

    internal void SetAbraxasAuthIfNotAuthenticated()
    {
        if (!_auth.IsAuthenticated)
        {
            _authStore.SetValues(string.Empty, new() { Loginid = _config.SecureConnect.ServiceUserId }, new() { Id = _config.SecureConnect.AbraxasTenantId }, Enumerable.Empty<string>());
        }
    }

    private async Task<bool> CanAccessDomainOfInfluence(DomainOfInfluence domainOfInfluence)
    {
        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadAll))
        {
            return true;
        }

        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadSameCanton))
        {
            if (await IsOwnerOfCanton(domainOfInfluence.Canton))
            {
                return true;
            }
        }

        _auth.EnsurePermission(Permissions.DomainOfInfluence.ReadSameTenant);

        return await _permissionRepo
            .Query()
            .AnyAsync(p => p.TenantId == _auth.Tenant.Id && p.DomainOfInfluenceId == domainOfInfluence.Id);
    }
}
