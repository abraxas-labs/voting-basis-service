// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        var politicalBusiness = await QueryById(id)
            ?? throw new EntityNotFoundException(id);

        await PermissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(politicalBusiness.DomainOfInfluenceId, true);
        return politicalBusiness;
    }

    public async Task<List<TPoliticalBusiness>> ListOwnedByTenantForContest(Guid contestId)
    {
        await PermissionService.EnsureCanReadContest(contestId);

        var tenantId = Auth.Tenant.Id;

        return await Repo.Query()
            .IgnoreQueryFilters() // Deleted DOI should still work
            .Where(pb => pb.ContestId == contestId && pb.DomainOfInfluence!.SecureConnectId == tenantId)
            .Include(pb => pb.DomainOfInfluence)
            .OrderBy(pb => pb.PoliticalBusinessNumber)
            .ToListAsync();
    }

    protected abstract Task<TPoliticalBusiness> QueryById(Guid id);
}
