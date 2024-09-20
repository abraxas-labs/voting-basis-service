// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class DomainOfInfluenceHierarchyBuilder
{
    private readonly IDbRepository<DataContext, DomainOfInfluence> _repo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;

    public DomainOfInfluenceHierarchyBuilder(
        IDbRepository<DataContext, DomainOfInfluence> repo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo)
    {
        _repo = repo;
        _hierarchyRepo = hierarchyRepo;
    }

    internal async Task RebuildHierarchy()
    {
        var allDomainOfInfluences = await _repo.Query().ToListAsync();
        await RebuildHierarchy(allDomainOfInfluences);
    }

    /// <summary>
    /// Rebuild the domain of influence hierarchy and persist it to make querying the hierarchy easier.
    /// </summary>
    /// <param name="allDomainOfInfluences">A list of all domain of influences.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal async Task RebuildHierarchy(List<DomainOfInfluence> allDomainOfInfluences)
    {
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
