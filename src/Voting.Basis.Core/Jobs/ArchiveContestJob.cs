// (c) Copyright by Abraxas Informatik AG
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

internal class ArchiveContestJob : ContestStateJob
{
    private readonly ContestWriter _contestWriter;

    public ArchiveContestJob(
        ILogger<ArchiveContestJob> logger,
        IDbRepository<DataContext, Contest> contestRepo,
        IClock clock,
        PermissionService permissionService,
        ContestWriter contestWriter)
        : base(logger, contestRepo, clock, permissionService)
    {
        _contestWriter = contestWriter;
    }

    protected override IQueryable<Contest> BuildQuery(IQueryable<Contest> query, DateTime now)
        => query.Where(c => c.ArchivePer < now && c.State == ContestState.PastLocked);

    protected override Task<bool> SetContestState(Guid contestId)
        => _contestWriter.TryArchive(contestId);
}
