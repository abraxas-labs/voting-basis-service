// (c) Copyright 2024 by Abraxas Informatik AG
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

internal class EndContestTestingPhaseJob : ContestStateJob
{
    private readonly ContestWriter _contestWriter;

    public EndContestTestingPhaseJob(
        ILogger<EndContestTestingPhaseJob> logger,
        IDbRepository<DataContext, Contest> contestRepo,
        IClock clock,
        PermissionService permissionService,
        ContestWriter contestWriter)
        : base(logger, contestRepo, clock, permissionService)
    {
        _contestWriter = contestWriter;
    }

    protected override IQueryable<Contest> BuildQuery(IQueryable<Contest> query, DateTime endOfTestingPhaseAsOf)
        => query.Where(c => c.EndOfTestingPhase <= endOfTestingPhaseAsOf && c.State < ContestState.Active);

    protected override Task<bool> SetContestState(Guid contestId)
        => _contestWriter.TryEndTestingPhase(contestId);
}
