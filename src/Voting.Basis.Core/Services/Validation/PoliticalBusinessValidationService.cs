// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Services.Validation;

public class PoliticalBusinessValidationService
{
    private readonly PermissionService _permissionService;
    private readonly ContestReader _contestReader;
    private readonly DomainOfInfluenceHierarchyRepo _doiHierarchyRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePbRepo;

    public PoliticalBusinessValidationService(
        PermissionService permissionService,
        ContestReader contestReader,
        DomainOfInfluenceHierarchyRepo doiHierarchyRepo,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePbRepo)
    {
        _permissionService = permissionService;
        _contestReader = contestReader;
        _doiHierarchyRepo = doiHierarchyRepo;
        _doiRepo = doiRepo;
        _simplePbRepo = simplePbRepo;
    }

    public async Task EnsureValidEditData(
        Guid politicalBusinessId,
        Guid contestId,
        Guid domainOfInfluenceId,
        string politicalBusinessNumber,
        PoliticalBusinessType politicalBusinessType,
        int reportLevel)
    {
        // ensure user has read permissions of the contest
        var contest = await _contestReader.Get(contestId);
        await _permissionService.EnsureDomainOfInfluencesAreChildrenOrSelf(contest.DomainOfInfluenceId, domainOfInfluenceId);

        await EnsureUniquePoliticalBusinessNumber(politicalBusinessId, contestId, domainOfInfluenceId, politicalBusinessNumber, politicalBusinessType);
        await EnsureValidReportDomainOfInfluenceLevel(domainOfInfluenceId, reportLevel);
        await EnsureNotVirtualTopLevelDomainOfInfluence(domainOfInfluenceId);
    }

    public async Task EnsureUniquePoliticalBusinessNumber(
        Guid politicalBusinessId,
        Guid contestId,
        Guid domainOfInfluenceId,
        string politicalBusinessNumber,
        PoliticalBusinessType politicalBusinessType)
    {
        // majority elections and secondary majority elections cannot have the same political business number
        List<PoliticalBusinessType> politicalBusinessTypes =
            politicalBusinessType == PoliticalBusinessType.MajorityElection || politicalBusinessType == PoliticalBusinessType.SecondaryMajorityElection
                ? [PoliticalBusinessType.MajorityElection, PoliticalBusinessType.SecondaryMajorityElection]
                : [politicalBusinessType];

        var alreadyExists = await _simplePbRepo.Query()
            .AnyAsync(pb =>
                pb.Id != politicalBusinessId
                && pb.ContestId == contestId
                && pb.DomainOfInfluenceId == domainOfInfluenceId
                && politicalBusinessTypes.Contains(pb.BusinessType)
                && pb.PoliticalBusinessNumber == politicalBusinessNumber);

        if (alreadyExists)
        {
            throw new DuplicatedPoliticalBusinessNumberException(politicalBusinessNumber);
        }
    }

    /// <summary>
    /// Ensures that the report level is valid. It is "relative" to the domain of influence of the political business,
    /// meaning that 0 equals the same level as the domain of influence. 1 would mean the child domain of influences and so on.
    /// </summary>
    /// <param name="doiId">The domain of influence id of a political business.</param>
    /// <param name="reportLevel">The report level of the business.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task EnsureValidReportDomainOfInfluenceLevel(Guid doiId, int reportLevel)
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

    private async Task EnsureNotVirtualTopLevelDomainOfInfluence(Guid domainOfInfluenceId)
    {
        var domainOfInfluence = await _doiRepo.GetByKey(domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        if (domainOfInfluence.VirtualTopLevel)
        {
            throw new ValidationException(
                "A virtual top level domain of influence is not allowed for political businesses.");
        }
    }
}
