// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Jobs;

internal class PastLockedContestJob : ContestStateJob
{
    private readonly ContestWriter _contestWriter;

    public PastLockedContestJob(
        ILogger<PastLockedContestJob> logger,
        IDbRepository<DataContext, Contest> contestRepo,
        IClock clock,
        PermissionService permissionService,
        ContestWriter contestWriter)
        : base(logger, contestRepo, clock, permissionService)
    {
        _contestWriter = contestWriter;
    }

    protected override IQueryable<Contest> BuildQuery(IQueryable<Contest> query, DateTime now)
        => query.Where(c => c.PastLockPer <= now && (c.State == ContestState.Active || c.State == ContestState.PastUnlocked));

    protected override Task<bool> SetContestState(Guid contestId)
        => _contestWriter.TrySetPastLocked(contestId);
}
