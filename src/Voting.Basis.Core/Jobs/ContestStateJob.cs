// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Scheduler;

namespace Voting.Basis.Core.Jobs;

public abstract class ContestStateJob : IScheduledJob
{
    private readonly ILogger<ContestStateJob> _logger;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IClock _clock;
    private readonly PermissionService _permissionService;

    protected ContestStateJob(
        ILogger<ContestStateJob> logger,
        IDbRepository<DataContext, Contest> contestRepo,
        IClock clock,
        PermissionService permissionService)
    {
        _logger = logger;
        _contestRepo = contestRepo;
        _clock = clock;
        _permissionService = permissionService;
    }

    public async Task Run(CancellationToken ct)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        var contestIds = await BuildQuery(_contestRepo.Query(), _clock.UtcNow)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var id in contestIds)
        {
            try
            {
                if (await SetContestState(id))
                {
                    _logger.LogInformation("State of contest {Id} updated", id);
                }
                else
                {
                    _logger.LogInformation("Event for state update of contest {Id} seems to be fired already", id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "State update of contest {Id} failed", id);
            }
        }
    }

    protected abstract IQueryable<Contest> BuildQuery(IQueryable<Contest> query, DateTime referenceDateTime);

    protected abstract Task<bool> SetContestState(Guid contestId);
}
