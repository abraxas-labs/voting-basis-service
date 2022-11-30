// (c) Copyright 2022 by Abraxas Informatik AG
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
    private readonly IDbRepository<DataContext, DomainOfInfluence> _repo;
    private readonly DomainOfInfluencePermissionRepo _permissionsRepo;
    private readonly DomainOfInfluenceCountingCircleRepo _countingCircleRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluenceSnapshot> _snapshotRepo;
    private readonly DomainOfInfluenceCountingCircleSnapshotRepo _snapshotDoiCcRepo;

    public DomainOfInfluencePermissionBuilder(
        IDbRepository<DataContext, DomainOfInfluence> repo,
        DomainOfInfluenceCountingCircleRepo countingCircleRepo,
        DomainOfInfluencePermissionRepo permissionsRepo,
        IDbRepository<DataContext, DomainOfInfluenceSnapshot> snapshotRepo,
        DomainOfInfluenceCountingCircleSnapshotRepo snapshotDoiCcRepo)
    {
        _repo = repo;
        _countingCircleRepo = countingCircleRepo;
        _permissionsRepo = permissionsRepo;
        _snapshotRepo = snapshotRepo;
        _snapshotDoiCcRepo = snapshotDoiCcRepo;
    }

    internal async Task RebuildPermissionTree()
    {
        var allDomainOfInfluences = await _repo
            .Query()
            .ToListAsync();
        await RebuildPermissionTree(allDomainOfInfluences);
    }

    internal async Task RebuildPermissionTree(List<DomainOfInfluence> allDomainOfInfluences)
    {
        var countingCirclesByDomainOfInfluenceId = await _countingCircleRepo.CountingCirclesByDomainOfInfluenceId();
        var tree = DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences, countingCirclesByDomainOfInfluenceId);

        var allTenantIds = allDomainOfInfluences
            .Select(d => d.SecureConnectId)
            .Union(countingCirclesByDomainOfInfluenceId.Values.SelectMany(c =>
                c.Select(cc => cc.CountingCircle.ResponsibleAuthority.SecureConnectId)))
            .Distinct();
        var permissions = allTenantIds.SelectMany(tid => BuildEntriesForTenant(tree, tid)).ToList();
        await _permissionsRepo.Replace(permissions);
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

    private IEnumerable<DomainOfInfluencePermissionEntry> BuildEntriesForTenant(
        IEnumerable<DomainOfInfluence> entries,
        string tenantId)
    {
        var tenantEntries =
            new Dictionary<(string TenantID, Guid DomainOfInfluenceId), DomainOfInfluencePermissionEntry>();
        BuildEntriesForTenant(entries, tenantId, tenantEntries);
        return tenantEntries.Values;
    }

    private void BuildEntriesForTenant(
        IEnumerable<DomainOfInfluence> entries,
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
        DomainOfInfluence doi,
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
                DomainOfInfluenceId = doi.Id,
                CountingCircleIds = filteredCountingCircles.ConvertAll(c => c.CountingCircleId),
            };

            permissionEntries[(tenantId, doi.Id)] = entry;

            AddParentsToPermissions(doi, tenantId, permissionEntries);
        }

        BuildEntriesForTenant(doi.Children, tenantId, permissionEntries, hasDirectAccess);
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
}
