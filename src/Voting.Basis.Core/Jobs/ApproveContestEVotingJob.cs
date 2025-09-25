// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Scheduler;

namespace Voting.Basis.Core.Jobs;

internal class ApproveContestEVotingJob : IScheduledJob
{
    private readonly ILogger<ApproveContestEVotingJob> _logger;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IClock _clock;
    private readonly PermissionService _permissionService;
    private readonly ContestWriter _contestWriter;

    public ApproveContestEVotingJob(
        ILogger<ApproveContestEVotingJob> logger,
        IDbRepository<DataContext, Contest> contestRepo,
        IClock clock,
        PermissionService permissionService,
        ContestWriter contestWriter)
    {
        _logger = logger;
        _contestRepo = contestRepo;
        _clock = clock;
        _permissionService = permissionService;
        _contestWriter = contestWriter;
        _contestWriter = contestWriter;
    }

    public async Task Run(CancellationToken ct)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();

        var contestIds = await _contestRepo.Query()
            .Where(c => !c.EVotingApproved
                && c.EVoting
                && c.State != ContestState.PastLocked
                && c.State != ContestState.Archived
                && c.EVotingApprovalDueDate <= _clock.UtcNow)
            .Select(c => c.Id)
            .ToListAsync(ct);

        foreach (var contestId in contestIds)
        {
            var approved = await _contestWriter.TryApproveEVoting(contestId);
            if (!approved)
            {
                _logger.LogInformation("Could not approve e-voting for contest {ContestId}", contestId);
            }
        }
    }
}
