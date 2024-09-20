// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Services.Validation;

public class ContestValidationService
{
    private readonly IDbRepository<DataContext, Contest> _contestRepo;

    public ContestValidationService(IDbRepository<DataContext, Contest> contestRepo)
    {
        _contestRepo = contestRepo;
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
        return await _contestRepo.Query()
                   .Where(c => c.Id == contestId)
                   .Select(c => (ContestState?)c.State)
                   .FirstOrDefaultAsync()
               ?? throw new EntityNotFoundException(contestId);
    }
}
