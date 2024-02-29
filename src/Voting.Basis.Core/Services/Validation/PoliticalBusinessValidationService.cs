// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Data.Repositories;

namespace Voting.Basis.Core.Services.Validation;

public class PoliticalBusinessValidationService
{
    private readonly PermissionService _permissionService;
    private readonly ContestReader _contestReader;
    private readonly DomainOfInfluenceHierarchyRepo _doiHierarchyRepo;

    public PoliticalBusinessValidationService(PermissionService permissionService, ContestReader contestReader, DomainOfInfluenceHierarchyRepo doiHierarchyRepo)
    {
        _permissionService = permissionService;
        _contestReader = contestReader;
        _doiHierarchyRepo = doiHierarchyRepo;
    }

    public async Task EnsureValidEditData(Guid contestId, Guid politicalBusinessDomainOfInfluenceId)
    {
        // ensure user has read permissions of the contest
        var contest = await _contestReader.Get(contestId);
        await _permissionService.EnsureDomainOfInfluencesAreChildrenOrSelf(
            contest.DomainOfInfluenceId,
            politicalBusinessDomainOfInfluenceId);
    }

    /// <summary>
    /// Ensures that the report level is valid. It is "relative" to the domain of influence of the political business,
    /// meaning that 0 equals the same level as the domain of influence. 1 would mean the child domain of influences and so on.
    /// </summary>
    /// <param name="doiId">The domain of influence id of a political business.</param>
    /// <param name="reportLevel">The report level of the business.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureValidReportDomainOfInfluenceLevel(Guid doiId, int reportLevel)
    {
        // since there is at least one domain of influence, the first level is always valid
        // report level cannot be negative, checked by business rules
        if (reportLevel == 0)
        {
            return;
        }

        var matchingHierarchy = await _doiHierarchyRepo.Query()
            .FirstAsync(h => h.DomainOfInfluenceId == doiId);

        var domainOfInfluenceParentCount = matchingHierarchy.ParentIds.Count;

        // sadly, we cannot fetch the parent ids count directly from the database due to EF limitations
        var parentIdsList = await _doiHierarchyRepo.Query()
            .Where(h => matchingHierarchy.ChildIds.Contains(h.DomainOfInfluenceId))
            .Select(h => h.ParentIds)
            .Distinct()
            .ToListAsync();

        // If we have the following hiearchy: Bund - Kanton - Bezirk - Andere
        // and the politicial business domain of influence is Kanton,
        // then the "deepest" level is Andere = 2, since levels are zero-indexed.
        // Since the "Andere"-hierarchy has 3 parent ids, the following statement should return false (3 - 1 < 2 is false)
        if (parentIdsList.All(ids => ids.Count - domainOfInfluenceParentCount < reportLevel))
        {
            throw new ValidationException($"Report domainOfInfluence level {reportLevel} is invalid.");
        }
    }
}
