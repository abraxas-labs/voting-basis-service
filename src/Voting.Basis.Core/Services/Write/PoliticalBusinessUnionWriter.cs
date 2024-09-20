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
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Write;

public abstract class PoliticalBusinessUnionWriter<TPoliticalBusiness, TPoliticalBusinessUnion>
    where TPoliticalBusiness : PoliticalBusiness, new()
    where TPoliticalBusinessUnion : PoliticalBusinessUnion, new()
{
    protected PoliticalBusinessUnionWriter(
        IDbRepository<DataContext, TPoliticalBusinessUnion> repo,
        IDbRepository<DataContext, TPoliticalBusiness> politicalBusinessRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IAuth auth,
        ContestValidationService contestValidationService,
        PermissionService permissionService)
    {
        Repo = repo;
        PoliticalBusinessRepo = politicalBusinessRepo;
        ContestRepo = contestRepo;
        Auth = auth;
        ContestValidationService = contestValidationService;
        PermissionService = permissionService;
    }

    protected IDbRepository<DataContext, TPoliticalBusinessUnion> Repo { get; }

    protected IDbRepository<DataContext, TPoliticalBusiness> PoliticalBusinessRepo { get; }

    protected IDbRepository<DataContext, Contest> ContestRepo { get; }

    protected IAuth Auth { get; }

    protected ContestValidationService ContestValidationService { get; }

    protected PermissionService PermissionService { get; }

    protected abstract PoliticalBusinessUnionType UnionType { get; }

    protected async Task EnsureCanCreatePoliticalBusinessUnion(Guid contestId)
    {
        var contest = await ContestRepo.Query()
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == contestId)
            ?? throw new EntityNotFoundException(contestId);

        ContestValidationService.EnsureInTestingPhase(contest);
        await PermissionService.EnsureCanReadContest(contest);

        if (!contest.DomainOfInfluence.CantonDefaults.EnabledPoliticalBusinessUnionTypes.Contains(UnionType))
        {
            throw new ValidationException($"{UnionType} is not enabled for this canton");
        }
    }

    protected async Task EnsureCanModifyPoliticalBusinessUnion(Guid politicalBusinessUnionId)
    {
        var politicalBusinessUnion = await Repo.GetByKey(politicalBusinessUnionId)
            ?? throw new EntityNotFoundException(politicalBusinessUnionId);

        await ContestValidationService.EnsureInTestingPhase(politicalBusinessUnion.ContestId);

        if (politicalBusinessUnion.SecureConnectId != Auth.Tenant.Id)
        {
            throw new ForbiddenException("Only owner of the political business union can edit");
        }
    }

    protected async Task EnsureValidPoliticalBusinessIds(Guid contestId, List<Guid> politicalBusinessIds)
    {
        if (politicalBusinessIds.Distinct().Count() != politicalBusinessIds.Count)
        {
            throw new ValidationException("duplicate political business id");
        }

        var tenantId = Auth.Tenant.Id;
        var anyPbFromDifferentTenantOrDifferentContest = await PoliticalBusinessRepo.Query()
            .Include(pb => pb.DomainOfInfluence)
            .Where(pb => politicalBusinessIds.Contains(pb.Id))
            .AnyAsync(pb => pb.DomainOfInfluence!.SecureConnectId != tenantId || pb.ContestId != contestId);

        if (anyPbFromDifferentTenantOrDifferentContest)
        {
            throw new ValidationException("cannot assign a political business from a different tenant or different contest to a political business union");
        }
    }
}
