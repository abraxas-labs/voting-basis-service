// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Services.Validation;

public class ContestValidationService
{
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IClock _clock;

    public ContestValidationService(IDbRepository<DataContext, Contest> contestRepo, IClock clock)
    {
        _contestRepo = contestRepo;
        _clock = clock;
    }

    internal async Task EnsureInTestingPhase(Guid contestId)
    {
        var state = await GetContestState(contestId);
        if (state.TestingPhaseEnded())
        {
            throw new ContestTestingPhaseEndedException();
        }
    }

    internal void EnsureInTestingPhase(Contest contest)
    {
        if (contest.TestingPhaseEnded)
        {
            throw new ContestTestingPhaseEndedException();
        }
    }

    internal async Task<ContestState> EnsureNotLocked(Guid contestId)
    {
        var state = await GetContestState(contestId);

        if (state.IsLocked())
        {
            throw new ContestLockedException();
        }

        return state;
    }

    internal async Task EnsureCanChangePoliticalBusinessEVotingApproval(Guid contestId, bool approvalValue)
    {
        var contest = await _contestRepo.GetByKey(contestId)
            ?? throw new EntityNotFoundException(nameof(Contest), contestId);

        if (!contest.EVoting)
        {
            throw new ContestMissingEVotingException();
        }

        if (contest.EVotingApproved && !approvalValue)
        {
            throw new ValidationException("Cannot reset the e-voting approval when the contest is already approved");
        }
    }

    internal async Task EnsureContestNotSetAsPreviousContest(Guid contestId)
    {
        var isSetAsPreviousContest = await _contestRepo.Query()
            .AnyAsync(c => c.PreviousContestId == contestId);

        if (isSetAsPreviousContest)
        {
            throw new ContestSetAsPreviousContestException();
        }
    }

    internal async Task EnsureContestsInMergeNotSetAsPreviousContest(IEnumerable<Guid> contestIds)
    {
        var anySetAsPreviousContest = await _contestRepo.Query()
            .AnyAsync(c => c.PreviousContestId != null && contestIds.Contains(c.PreviousContestId.Value));

        if (anySetAsPreviousContest)
        {
            throw new ContestInMergeSetAsPreviousContestException();
        }
    }

    private async Task<ContestState> GetContestState(Guid contestId)
    {
        var contestInfo = await _contestRepo.Query()
            .Where(c => c.Id == contestId)
            .Select(c => new
            {
                c.State,
                c.EndOfTestingPhase,
            })
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(contestId);

        var state = contestInfo.State;
        if (state == ContestState.TestingPhase && contestInfo.EndOfTestingPhase <= _clock.UtcNow)
        {
            // The job to set the state to active may not have run yet, so we patch this manually
            state = ContestState.Active;
        }

        return state;
    }
}
