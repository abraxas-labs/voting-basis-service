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

internal class ActivateCountingCirclesMergeJob : IScheduledJob
{
    private readonly ILogger<ActivateCountingCirclesMergeJob> _logger;
    private readonly IDbRepository<DataContext, CountingCirclesMerger> _ccMergeRepo;
    private readonly IClock _clock;
    private readonly PermissionService _permissionService;
    private readonly CountingCircleWriter _writer;

    public ActivateCountingCirclesMergeJob(
        ILogger<ActivateCountingCirclesMergeJob> logger,
        IDbRepository<DataContext, CountingCirclesMerger> ccMergeRepo,
        IClock clock,
        PermissionService permissionService,
        CountingCircleWriter writer)
    {
        _logger = logger;
        _ccMergeRepo = ccMergeRepo;
        _clock = clock;
        _permissionService = permissionService;
        _writer = writer;
    }

    public async Task Run(CancellationToken ct)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();

        // IgnoreQueryFilters() is required, otherwise it will return an empty collection because newCountingCircle has state inactive
        var ccMerges = await _ccMergeRepo.Query()
            .IgnoreQueryFilters()
            .Include(x => x.NewCountingCircle)
            .Where(x => !x.Merged && x.ActiveFrom < _clock.UtcNow)
            .ToListAsync(ct);

        foreach (var ccMerge in ccMerges)
        {
            try
            {
                var activated = await _writer.TryActivateMerge(ccMerge.NewCountingCircle!.Id);
                if (!activated)
                {
                    _logger.LogInformation("Could not activate counting circle merger with id {Id}", ccMerge.Id);
                    continue;
                }

                _logger.LogInformation("Activate counting circles merge {id} done", ccMerge.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Counting circles merge {id} failed", ccMerge.Id);
            }
        }
    }
}
