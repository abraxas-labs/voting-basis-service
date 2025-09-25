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

internal class ApprovePoliticalBusinessEVotingJob : IScheduledJob
{
    private readonly ILogger<ApprovePoliticalBusinessEVotingJob> _logger;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly IClock _clock;
    private readonly PermissionService _permissionService;
    private readonly VoteWriter _voteWriter;
    private readonly ProportionalElectionWriter _proportionalElectionWriter;
    private readonly MajorityElectionWriter _majorityElectionWriter;

    public ApprovePoliticalBusinessEVotingJob(
        ILogger<ApprovePoliticalBusinessEVotingJob> logger,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo,
        IClock clock,
        PermissionService permissionService,
        VoteWriter voteWriter,
        ProportionalElectionWriter proportionalElectionWriter,
        MajorityElectionWriter majorityElectionWriter)
    {
        _logger = logger;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
        _clock = clock;
        _permissionService = permissionService;
        _voteWriter = voteWriter;
        _proportionalElectionWriter = proportionalElectionWriter;
        _majorityElectionWriter = majorityElectionWriter;
    }

    public async Task Run(CancellationToken ct)
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();

        var pbs = await _simplePoliticalBusinessRepo.Query()
            .Where(pb => pb.EVotingApproved == false
                && pb.Contest.EVoting
                && pb.Contest.State != ContestState.PastLocked
                && pb.Contest.State != ContestState.Archived
                && (pb.Contest.EVotingApproved || pb.Contest.EVotingTo <= _clock.UtcNow))
            .ToListAsync(ct);

        foreach (var pb in pbs)
        {
            await ApproveEVoting(pb);
        }
    }

    private async Task ApproveEVoting(SimplePoliticalBusiness pb)
    {
        var tryApproveTask = pb.BusinessType switch
        {
            PoliticalBusinessType.Vote => _voteWriter.TryApproveEVoting(pb.Id),
            PoliticalBusinessType.ProportionalElection => _proportionalElectionWriter.TryApproveEVoting(pb.Id),
            PoliticalBusinessType.MajorityElection => _majorityElectionWriter.TryApproveEVoting(pb.Id),
            PoliticalBusinessType.SecondaryMajorityElection => _majorityElectionWriter.TryApproveSecondaryMajorityElectionEVoting(pb.Id),
            _ => throw new InvalidOperationException(),
        };

        var approved = await tryApproveTask;
        if (!approved)
        {
            _logger.LogInformation("Could not approve e-voting for political business {PoliticalBusinessId}", pb.Id);
        }
    }
}
