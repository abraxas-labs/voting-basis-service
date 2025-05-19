// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class DomainOfInfluenceHierarchyBuilder
{
    private readonly DomainOfInfluenceRepo _repo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;

    public DomainOfInfluenceHierarchyBuilder(
        DomainOfInfluenceRepo repo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo)
    {
        _repo = repo;
        _hierarchyRepo = hierarchyRepo;
    }

    internal async Task RemoveDomainOfInfluences(List<Guid> ids)
    {
        await _hierarchyRepo.Query()
            .Where(x => ids.Contains(x.DomainOfInfluenceId))
            .ExecuteDeleteAsync();
        await _hierarchyRepo.RemoveIdsFromChildIdsEntries(ids);
    }

    internal async Task InsertDomainOfInfluence(DomainOfInfluence domainOfInfluence)
    {
        var parentIds = new List<Guid>();

        if (domainOfInfluence.ParentId.HasValue)
        {
            parentIds = await _hierarchyRepo.GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(domainOfInfluence.ParentId.Value);
            await _hierarchyRepo.Query()
                .Where(x => parentIds.Contains(x.DomainOfInfluenceId))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ChildIds, x => x.ChildIds.Append(domainOfInfluence.Id)));
        }

        await _hierarchyRepo.Create(new DomainOfInfluenceHierarchy
        {
            DomainOfInfluenceId = domainOfInfluence.Id,
            TenantId = domainOfInfluence.SecureConnectId,
            ParentIds = parentIds,
        });
    }

    internal async Task UpdateDomainOfInfluence(Guid id, string newTenantId, string oldTenantId)
    {
        await _hierarchyRepo.Query()
            .Where(x => x.DomainOfInfluenceId == id && x.TenantId == oldTenantId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TenantId, newTenantId));
    }

    // This methods is pretty inefficient and should only be used tests, because
    // there we only call it once (which is faster than calling the Insert/Update methods multiple times)
    internal async Task RebuildHierarchy()
    {
        var allDomainOfInfluences = await _repo.GetAllSlim();
        DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences);
        var hierarchies = allDomainOfInfluences.Select(BuildHierarchyForDomainOfInfluence);
        await _hierarchyRepo.Replace(hierarchies);
    }

    private DomainOfInfluenceHierarchy BuildHierarchyForDomainOfInfluence(DomainOfInfluence doi)
    {
        return new DomainOfInfluenceHierarchy
        {
            DomainOfInfluenceId = doi.Id,
            TenantId = doi.SecureConnectId,
            ChildIds = FindChildIds(doi).ToList(),
            ParentIds = FindParentIds(doi).ToList(),
        };
    }

    private IEnumerable<Guid> FindChildIds(DomainOfInfluence doi)
    {
        foreach (var child in doi.Children)
        {
            yield return child.Id;

            foreach (var subChildId in FindChildIds(child))
            {
                yield return subChildId;
            }
        }
    }

    private IEnumerable<Guid> FindParentIds(DomainOfInfluence doi)
    {
        while (doi.Parent != null)
        {
            doi = doi.Parent;
            yield return doi.Id;
        }
    }
}
