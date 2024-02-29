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
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class ElectionGroupReader
{
    private readonly IDbRepository<DataContext, ElectionGroup> _repo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IAuth _auth;
    private readonly PermissionService _permissionService;

    public ElectionGroupReader(
        IDbRepository<DataContext, ElectionGroup> repo,
        IDbRepository<DataContext, Contest> contestRepo,
        IAuth auth,
        PermissionService permissionService)
    {
        _repo = repo;
        _contestRepo = contestRepo;
        _auth = auth;
        _permissionService = permissionService;
    }

    public async Task<IEnumerable<ElectionGroup>> List(Guid contestId)
    {
        var canReadAll = _auth.HasPermission(Permissions.ElectionGroup.ReadAll);
        var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
        var contest = await _contestRepo.Query()
            .FirstOrDefaultAsync(c => c.Id == contestId && (canReadAll || doiHierarchyGroups.AccessibleDoiIds.Contains(c.DomainOfInfluenceId)));

        if (contest == null)
        {
            throw new EntityNotFoundException(contestId);
        }

        return await _repo.Query()
            .Include(eg => eg.PrimaryMajorityElection)
            .Include(eg => eg.SecondaryMajorityElections)
            .Where(eg => eg.PrimaryMajorityElection.ContestId == contestId
                && (canReadAll || doiHierarchyGroups.AccessibleDoiIds.Contains(eg.PrimaryMajorityElection.DomainOfInfluenceId)))
            .OrderBy(eg => eg.Number)
            .ToListAsync();
    }
}
