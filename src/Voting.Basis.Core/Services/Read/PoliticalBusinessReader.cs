// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public abstract class PoliticalBusinessReader<TPoliticalBusiness>
    where TPoliticalBusiness : PoliticalBusiness, new()
{
    protected PoliticalBusinessReader(
        IAuth auth,
        PermissionService permissionService,
        IDbRepository<DataContext, TPoliticalBusiness> repo)
    {
        Auth = auth;
        PermissionService = permissionService;
        Repo = repo;
    }

    protected IAuth Auth { get; }

    protected IDbRepository<DataContext, TPoliticalBusiness> Repo { get; }

    protected PermissionService PermissionService { get; }

    public async Task<TPoliticalBusiness> Get(Guid id)
    {
        Auth.EnsureAdminOrElectionAdmin();

        var politicalBusiness = await QueryById(id)
            ?? throw new EntityNotFoundException(id);

        await PermissionService.EnsureIsOwnerOfDomainOfInfluence(politicalBusiness.DomainOfInfluenceId);
        return politicalBusiness;
    }

    public async Task<List<TPoliticalBusiness>> ListOwnedByTenantForContest(Guid contestId)
    {
        Auth.EnsureAdminOrElectionAdmin();
        await PermissionService.EnsureCanReadContest(contestId);

        var tenantId = Auth.Tenant.Id;

        return await Repo.Query()
            .Where(pb => pb.ContestId == contestId && pb.DomainOfInfluence!.SecureConnectId == tenantId)
            .Include(pb => pb.DomainOfInfluence)
            .OrderBy(pb => pb.InternalDescription)
            .ToListAsync();
    }

    protected abstract Task<TPoliticalBusiness> QueryById(Guid id);
}
