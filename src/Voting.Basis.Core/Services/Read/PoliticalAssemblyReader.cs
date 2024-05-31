// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class PoliticalAssemblyReader
{
    private readonly IDbRepository<DataContext, PoliticalAssembly> _repo;
    private readonly IAuth _auth;
    private readonly PermissionService _permissionService;
    private readonly IClock _clock;

    public PoliticalAssemblyReader(
        IDbRepository<DataContext, PoliticalAssembly> repo,
        IAuth auth,
        PermissionService permissionService,
        IClock clock)
    {
        _repo = repo;
        _auth = auth;
        _permissionService = permissionService;
        _clock = clock;
    }

    public async Task<PoliticalAssembly> Get(Guid id)
    {
        var query = _repo.Query().AsSplitQuery();

        if (!_auth.HasAnyPermission(Permissions.PoliticalAssembly.ReadSameCanton, Permissions.PoliticalAssembly.ReadAll))
        {
            var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
            query = query
                .Where(x => doiHierarchyGroups.TenantAndParentDoiIds.Contains(x.DomainOfInfluenceId))
                .Include(x => x.DomainOfInfluence);
        }

        var politicalAssembly = await query
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(nameof(PoliticalAssembly), id);

        if (_auth.HasPermission(Permissions.PoliticalAssembly.ReadSameCanton)
            && !await _permissionService.IsOwnerOfCanton(politicalAssembly.DomainOfInfluence.Canton))
        {
            throw new EntityNotFoundException(nameof(PoliticalAssembly), id);
        }

        return politicalAssembly;
    }

    public async Task<IEnumerable<PoliticalAssembly>> List()
    {
        var query = _repo.Query().AsSplitQuery();

        if (!_auth.HasAnyPermission(Permissions.PoliticalAssembly.ReadSameCanton, Permissions.PoliticalAssembly.ReadAll))
        {
            var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
            query = query
                .Where(x => doiHierarchyGroups.TenantAndParentDoiIds.Contains(x.DomainOfInfluenceId))
                .Include(x => x.DomainOfInfluence);
        }

        var politicalAssemblies = await query
            .Include(x => x.DomainOfInfluence)
            .Where(x => x.Date >= _clock.UtcNow)
            .ToListAsync();

        foreach (var politicalAssembly in politicalAssemblies)
        {
            if (_auth.HasPermission(Permissions.PoliticalAssembly.ReadSameCanton) &&
                !await _permissionService.IsOwnerOfCanton(politicalAssembly.DomainOfInfluence.Canton))
            {
                throw new EntityNotFoundException(nameof(PoliticalAssembly), politicalAssembly.Id);
            }
        }

        return politicalAssemblies;
    }
}
