// (c) Copyright by Abraxas Informatik AG
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

public abstract class PoliticalAssemblyStateJob : IScheduledJob
{
    private readonly ILogger<PoliticalAssemblyStateJob> _logger;
    private readonly IDbRepository<DataContext, PoliticalAssembly> _politicalAssemblyRepo;
    private readonly IClock _clock;
    private readonly PermissionService _permissionService;

    protected PoliticalAssemblyStateJob(
        ILogger<PoliticalAssemblyStateJob> logger,
        IDbRepository<DataContext, PoliticalAssembly> politicalAssemblyRepo,
        IClock clock,
        PermissionService permissionService)
    {
        _logger = logger;
        _politicalAssemblyRepo = politicalAssemblyRepo;
        _clock = clock;
        _permissionService = permissionService;
    }

    public async Task Run(CancellationToken ct)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();
        var politicalAssemblyIds = await BuildQuery(_politicalAssemblyRepo.Query(), _clock.UtcNow)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var id in politicalAssemblyIds)
        {
            try
            {
                if (await SetPoliticalAssemblyState(id))
                {
                    _logger.LogInformation("State of political assembly {Id} updated", id);
                }
                else
                {
                    _logger.LogInformation("Event for state update of political assembly {Id} seems to be fired already", id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "State update of political assembly {Id} failed", id);
            }
        }
    }

    protected abstract IQueryable<PoliticalAssembly> BuildQuery(IQueryable<PoliticalAssembly> query, DateTime referenceDateTime);

    protected abstract Task<bool> SetPoliticalAssemblyState(Guid politicalAssemblyId);
}
