// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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

public class ActivateCountingCircleEVotingJob : IScheduledJob
{
    private readonly ILogger<ActivateCountingCircleEVotingJob> _logger;
    private readonly IDbRepository<DataContext, CountingCircle> _ccRepo;
    private readonly IClock _clock;
    private readonly PermissionService _permissionService;
    private readonly CountingCircleWriter _writer;

    public ActivateCountingCircleEVotingJob(
        ILogger<ActivateCountingCircleEVotingJob> logger,
        IDbRepository<DataContext, CountingCircle> ccRepo,
        IClock clock,
        PermissionService permissionService,
        CountingCircleWriter writer)
    {
        _logger = logger;
        _ccRepo = ccRepo;
        _clock = clock;
        _permissionService = permissionService;
        _writer = writer;
    }

    public async Task Run(CancellationToken ct)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();

        var now = _clock.UtcNow.ConvertUtcTimeToSwissTime();

        var ccs = await _ccRepo.Query()
            .Where(x => !x.EVoting && x.EVotingActiveFrom <= now)
            .ToListAsync(ct);

        foreach (var cc in ccs)
        {
            try
            {
                var activated = await _writer.TryActivateEVoting(cc.Id);
                if (!activated)
                {
                    _logger.LogInformation("Could not activate e-voting for counting circle id {Id}", cc.Id);
                    continue;
                }

                _logger.LogInformation("Activate e-voting for counting circle {id} done", cc.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Activate e-voting for Counting circle {id} failed", cc.Id);
            }
        }
    }
}
