// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class ContestCountingCircleOptionsReplacer
{
    private readonly ContestCountingCircleOptionsRepo _ccOptionsRepo;
    private readonly DomainOfInfluenceCountingCircleRepo _doiCountingCirclesRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;

    public ContestCountingCircleOptionsReplacer(
        ContestCountingCircleOptionsRepo ccOptionsRepo,
        DomainOfInfluenceCountingCircleRepo doiCountingCirclesRepo,
        IDbRepository<DataContext, Contest> contestRepo)
    {
        _ccOptionsRepo = ccOptionsRepo;
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
        _contestRepo = contestRepo;
    }

    internal async Task ReplaceForContestsInTestingPhaseAndDoiIds(IEnumerable<Guid> doiIds)
    {
        var contests = await _contestRepo.Query()
            .AsSplitQuery()
            .Where(x => x.State == ContestState.TestingPhase && doiIds.Contains(x.DomainOfInfluenceId))
            .Include(x => x.CountingCircleOptions)
            .Include(x => x.DomainOfInfluence.CountingCircles)
            .ToListAsync();

        var options = contests.SelectMany(c => BuildOptions(
            c,
            c.DomainOfInfluence.CountingCircles.Select(x => x.CountingCircleId)));
        await _ccOptionsRepo.Replace(contests.Select(x => x.Id), options);
    }

    internal async Task Replace(Contest contest, bool? eVoting = null)
    {
        var ccIds = await _doiCountingCirclesRepo.GetCountingCircleGuidsByDomainOfInfluenceId(contest.DomainOfInfluenceId);
        var options = BuildOptions(contest, ccIds, eVoting);
        await _ccOptionsRepo.Replace(contest.Id, options);
    }

    /// <summary>
    /// Builds contest counting circle options.
    /// </summary>
    /// <param name="contest">The contest.</param>
    /// <param name="countingCircleIds">A list of all counting circle ids which are part of this contest.</param>
    /// <param name="eVoting">
    /// If null, existing values are used if present or false if no value exists yet.
    /// If a value is set all options eVoting flag is set according to the provided value.
    /// </param>
    /// <returns>An enumerable with all contest counting circle options.</returns>
    private IEnumerable<ContestCountingCircleOption> BuildOptions(Contest contest, IEnumerable<Guid> countingCircleIds, bool? eVoting = null)
    {
        var existingValues = eVoting != null
            ? new Dictionary<Guid, bool>()
            : contest.CountingCircleOptions.ToDictionary(x => x.CountingCircleId, x => x.EVoting);

        return countingCircleIds.Select(ccId => new ContestCountingCircleOption
        {
            ContestId = contest.Id,
            CountingCircleId = ccId,
            EVoting = existingValues.GetValueOrDefault(ccId, eVoting ?? false),
        });
    }
}
