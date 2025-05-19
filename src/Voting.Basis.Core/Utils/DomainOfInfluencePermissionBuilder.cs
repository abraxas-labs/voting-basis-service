// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Data.Repositories.Snapshot;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Utils;

public class DomainOfInfluencePermissionBuilder
{
    private readonly DomainOfInfluenceRepo _repo;
    private readonly DomainOfInfluencePermissionRepo _permissionsRepo;
    private readonly DomainOfInfluenceCountingCircleRepo _countingCircleRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluenceSnapshot> _snapshotRepo;
    private readonly DomainOfInfluenceCountingCircleSnapshotRepo _snapshotDoiCcRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;

    public DomainOfInfluencePermissionBuilder(
        DomainOfInfluenceRepo repo,
        DomainOfInfluenceCountingCircleRepo countingCircleRepo,
        DomainOfInfluencePermissionRepo permissionsRepo,
        IDbRepository<DataContext, DomainOfInfluenceSnapshot> snapshotRepo,
        DomainOfInfluenceCountingCircleSnapshotRepo snapshotDoiCcRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo)
    {
        _repo = repo;
        _countingCircleRepo = countingCircleRepo;
        _permissionsRepo = permissionsRepo;
        _snapshotRepo = snapshotRepo;
        _snapshotDoiCcRepo = snapshotDoiCcRepo;
        _hierarchyRepo = hierarchyRepo;
    }

    internal async Task BuildPermissionTreeForNewDomainOfInfluence(DomainOfInfluence doi)
    {
        var affectedTenants = await _hierarchyRepo.Query()
            .Where(x => x.ChildIds.Contains(doi.Id))
            .Select(x => x.TenantId)
            .ToHashSetAsync();
        affectedTenants.Add(doi.SecureConnectId);

        await _permissionsRepo.CreateRange(affectedTenants.Select(x => new DomainOfInfluencePermissionEntry
        {
            IsParent = false,
            TenantId = x,
            DomainOfInfluenceId = doi.Id,
        }));
    }

    /// <summary>
    /// This method rebuilds the whole permission tree, which is very inefficient if used often.
    /// Use the other variants if possible.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal async Task RebuildPermissionTree()
    {
        var allDomainOfInfluences = await _repo.GetAllSlim();
        var countingCirclesByDomainOfInfluenceId = await CountingCirclesByDomainOfInfluenceId();
        var tree = DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences);

        var allTenants = allDomainOfInfluences
            .Select(d => d.SecureConnectId)
            .Union(countingCirclesByDomainOfInfluenceId.Values.SelectMany(c => c.Select(cc => cc.TenantId)))
            .ToHashSet();
        var permissions = allTenants.SelectMany(tid => BuildEntriesForTenant(tree, tid, countingCirclesByDomainOfInfluenceId));

        await _permissionsRepo.Replace(permissions.ToList());
    }

    internal async Task RebuildPermissionTreeForDomainOfInfluence(Guid domainOfInfluenceId, HashSet<string>? affectedTenants = null)
    {
        var hierarchyEntry = await _hierarchyRepo.Query().FirstAsync(x => x.DomainOfInfluenceId == domainOfInfluenceId);
        await RebuildPermissionTreeForDomainOfInfluence(hierarchyEntry, affectedTenants);
    }

    internal async Task RebuildPermissionTreeForDomainOfInfluence(DomainOfInfluenceHierarchy affectedHierarchy, HashSet<string>? affectedTenants = null)
    {
        var doisFromHierarchy = affectedHierarchy.ChildIds
            .Concat(affectedHierarchy.ParentIds)
            .Append(affectedHierarchy.DomainOfInfluenceId)
            .ToHashSet();
        await RebuildPermissionTree(doisFromHierarchy, affectedTenants);
    }

    internal async Task RebuildPermissionTreeForCountingCircle(Guid countingCircleId, HashSet<string>? affectedTenants = null)
    {
        var doiIds = await _countingCircleRepo.Query()
            .Where(x => x.CountingCircleId == countingCircleId)
            .Select(x => x.DomainOfInfluenceId)
            .Distinct()
            .ToHashSetAsync();
        await RebuildPermissionTree(doiIds, affectedTenants);
    }

    internal async Task<IEnumerable<DomainOfInfluencePermissionEntry>> GetPermissionTreeSnapshot(
        DateTime dateTime,
        bool includeDeletedDomainOfInfluences,
        bool includeDeletedCountingCircles)
    {
        var allDomainOfInfluences = await _snapshotRepo
            .Query()
            .ValidOn(dateTime)
            .Where(x => includeDeletedDomainOfInfluences || !x.Deleted)
            .ToListAsync();
        var countingCirclesByDomainOfInfluenceId = await _snapshotDoiCcRepo.CountingCirclesByDomainOfInfluenceId(dateTime, includeDeletedCountingCircles);

        return GetPermissionTreeSnapshot(allDomainOfInfluences, countingCirclesByDomainOfInfluenceId);
    }

    internal IEnumerable<DomainOfInfluencePermissionEntry> GetPermissionTreeSnapshot(
        List<DomainOfInfluenceSnapshot> allDomainOfInfluences,
        Dictionary<Guid, List<DomainOfInfluenceCountingCircleSnapshot>> countingCirclesByDomainOfInfluenceId)
    {
        var tree = DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences, countingCirclesByDomainOfInfluenceId);

        var allTenantIds = allDomainOfInfluences
            .Select(d => d.SecureConnectId)
            .Union(countingCirclesByDomainOfInfluenceId.Values.SelectMany(c =>
                c.Select(cc => cc.CountingCircle.ResponsibleAuthority.SecureConnectId)));

        var tenantIds = allTenantIds.SelectMany(tid => BuildEntriesForTenant(tree, tid)).ToList();

        // Ensures that methods which use the same doi references have clean references
        foreach (var doi in allDomainOfInfluences)
        {
            doi.Children = new HashSet<DomainOfInfluenceSnapshot>();
        }

        return tenantIds;
    }

    private async Task RebuildPermissionTree(HashSet<Guid>? affectedDomainOfInfluences, HashSet<string>? affectedTenants)
    {
        // Need to build the whole tree in all cases, otherwise we do not have enough information.
        // For example removing a counting circle may or may not remove it from the root DOI, depending if
        // other sub-DOIs also have it assigned.
        var allDomainOfInfluences = await _repo.GetAllSlim();
        var countingCirclesByDomainOfInfluenceId = await CountingCirclesByDomainOfInfluenceId();
        var tree = DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences);

        var tenants = affectedTenants ?? allDomainOfInfluences
            .Select(d => d.SecureConnectId)
            .Union(countingCirclesByDomainOfInfluenceId.Values.SelectMany(c => c.Select(cc => cc.TenantId)))
            .ToHashSet();
        var permissions = tenants.SelectMany(tid => BuildEntriesForTenant(tree, tid, countingCirclesByDomainOfInfluenceId));

        var query = _permissionsRepo.Query();
        if (affectedDomainOfInfluences != null)
        {
            permissions = permissions.Where(x => affectedDomainOfInfluences.Contains(x.DomainOfInfluenceId));
            query = query.Where(x => affectedDomainOfInfluences.Contains(x.DomainOfInfluenceId));
        }

        if (affectedTenants != null)
        {
            query = query.Where(x => affectedTenants.Contains(x.TenantId));
        }

        // Only delete the existing permissions if something would be affected
        // Otherwise we would delete all permissions accidentally
        if (affectedDomainOfInfluences?.Count > 0 || affectedTenants?.Count > 0)
        {
            await query.ExecuteDeleteAsync();
        }

        var toInsert = permissions.ToArray();
        if (toInsert.Length > 0)
        {
            await _permissionsRepo.CreateRange(toInsert);
        }
    }

    private IEnumerable<DomainOfInfluencePermissionEntry> BuildEntriesForTenant(
        IEnumerable<DomainOfInfluence> entries,
        string tenantId,
        Dictionary<Guid, List<(Guid CountingCircleId, string TenantId)>> countingCirclesByDoiId)
    {
        var tenantEntries =
            new Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry>();
        BuildEntriesForTenant(entries, tenantId, tenantEntries, countingCirclesByDoiId);
        return tenantEntries.Values;
    }

    private void BuildEntriesForTenant(
        IEnumerable<DomainOfInfluence> entries,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries,
        Dictionary<Guid, List<(Guid CountingCircleId, string TenantId)>> countingCirclesByDoiId,
        bool hasAccessToParent = false)
    {
        foreach (var entry in entries)
        {
            BuildEntriesForTenant(entry, tenantId, permissionEntries, countingCirclesByDoiId, hasAccessToParent);
        }
    }

    private void BuildEntriesForTenant(
        DomainOfInfluence doi,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries,
        Dictionary<Guid, List<(Guid CountingCircleId, string TenantId)>> countingCirclesByDoiId,
        bool hasAccessToParent = false)
    {
        var hasDirectAccess = doi.SecureConnectId == tenantId || hasAccessToParent;
        var filteredCountingCircles = countingCirclesByDoiId.GetValueOrDefault(doi.Id, [])
            .Where(c => hasDirectAccess || c.TenantId == tenantId)
            .Select(c => c.CountingCircleId)
            .Distinct()
            .ToList();

        if (hasDirectAccess || filteredCountingCircles.Count > 0)
        {
            var entry = new DomainOfInfluencePermissionEntry
            {
                IsParent = !hasDirectAccess,
                TenantId = tenantId,
                DomainOfInfluenceId = doi.Id,
                CountingCircleIds = filteredCountingCircles,
            };

            permissionEntries[(tenantId, doi.Id)] = entry;

            AddParentsToPermissions(doi, tenantId, permissionEntries);
        }

        BuildEntriesForTenant(doi.Children, tenantId, permissionEntries, countingCirclesByDoiId, hasDirectAccess);
    }

    private void AddParentsToPermissions(
        DomainOfInfluence doi,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries)
    {
        var currentParent = doi.Parent;
        while (currentParent != null)
        {
            var key = (tenantId, currentParent.Id);
            if (permissionEntries.ContainsKey(key))
            {
                return;
            }

            permissionEntries[key] =
                new DomainOfInfluencePermissionEntry
                {
                    IsParent = true,
                    TenantId = tenantId,
                    DomainOfInfluenceId = currentParent.Id,
                };
            currentParent = currentParent.Parent;
        }
    }

    private IEnumerable<DomainOfInfluencePermissionEntry> BuildEntriesForTenant(
        IEnumerable<DomainOfInfluenceSnapshot> entries,
        string tenantId)
    {
        var tenantEntries =
            new Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry>();
        BuildEntriesForTenant(entries, tenantId, tenantEntries);
        return tenantEntries.Values;
    }

    private void BuildEntriesForTenant(
        IEnumerable<DomainOfInfluenceSnapshot> entries,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries,
        bool hasAccessToParent = false)
    {
        foreach (var entry in entries)
        {
            BuildEntriesForTenant(entry, tenantId, permissionEntries, hasAccessToParent);
        }
    }

    private void BuildEntriesForTenant(
        DomainOfInfluenceSnapshot doi,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries,
        bool hasAccessToParent = false)
    {
        var hasDirectAccess = doi.SecureConnectId == tenantId || hasAccessToParent;
        var filteredCountingCircles = doi.CountingCircles
            .Where(c => hasDirectAccess || c.CountingCircle.ResponsibleAuthority.SecureConnectId == tenantId)
            .ToList();

        if (hasDirectAccess || filteredCountingCircles.Count > 0)
        {
            var entry = new DomainOfInfluencePermissionEntry
            {
                IsParent = !hasDirectAccess,
                TenantId = tenantId,
                DomainOfInfluenceId = doi.BasisId,
                CountingCircleIds = filteredCountingCircles.ConvertAll(c => c.BasisCountingCircleId),
            };

            permissionEntries[(tenantId, doi.Id)] = entry;

            AddParentsToPermissions(doi, tenantId, permissionEntries);
        }

        BuildEntriesForTenant(doi.Children, tenantId, permissionEntries, hasDirectAccess);
    }

    private void AddParentsToPermissions(
        DomainOfInfluenceSnapshot doi,
        string tenantId,
        Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry> permissionEntries)
    {
        var currentParent = doi.Parent;
        while (currentParent != null)
        {
            var key = (tenantId, currentParent.BasisId);
            if (permissionEntries.ContainsKey(key))
            {
                return;
            }

            permissionEntries[key] =
                new DomainOfInfluencePermissionEntry
                {
                    IsParent = true,
                    TenantId = tenantId,
                    DomainOfInfluenceId = currentParent.BasisId,
                };
            currentParent = currentParent.Parent;
        }
    }

    private async Task<Dictionary<Guid, List<(Guid CountingCircleId, string TenantId)>>> CountingCirclesByDomainOfInfluenceId()
    {
        var entries = await _countingCircleRepo.Query()
            .Where(x => x.CountingCircle.State == CountingCircleState.Active)
            .Select(x => new
            {
                x.DomainOfInfluenceId,
                x.CountingCircleId,
                TenantId = x.CountingCircle.ResponsibleAuthority.SecureConnectId,
            })
            .ToListAsync();

        return entries
            .GroupBy(x => x.DomainOfInfluenceId)
            .ToDictionary(x => x.Key, x => x.Select(y => (y.CountingCircleId, y.TenantId)).ToList());
    }
}
