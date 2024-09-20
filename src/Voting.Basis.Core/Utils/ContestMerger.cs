// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Basis.Core.Utils;

public class ContestMerger
{
    private readonly ContestValidationService _contestValidationService;
    private readonly IDbRepository<DataContext, Data.Models.Contest> _contestRepo;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ILogger<ContestMerger> _logger;

    public ContestMerger(
        ContestValidationService contestValidationService,
        IDbRepository<DataContext, Data.Models.Contest> contestRepo,
        IAggregateRepository aggregateRepository,
        ILogger<ContestMerger> logger)
    {
        _contestValidationService = contestValidationService;
        _contestRepo = contestRepo;
        _aggregateRepository = aggregateRepository;
        _logger = logger;
    }

    internal async Task MergeContests(
        ContestAggregate contestAggregate,
        Contest contest,
        IReadOnlyCollection<Guid> oldContestIds)
    {
        var isCreate = contest.Id == default;

        if (isCreate)
        {
            contest.Id = Guid.NewGuid();
        }

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["NewContestId"] = contest.Id,
        });

        await _contestValidationService.EnsureContestsInMergeNotSetAsPreviousContest(oldContestIds);

        if (oldContestIds.Count == 0)
        {
            throw new ValidationException($"Cannot merge contests {(isCreate ? "into a new contest" : "into contest " + contest.Id)} with no source ids");
        }

        var readModelContests = await LoadReadModelContestsWithChilds(oldContestIds);
        var contestChildAggregates = await GetContestChildAggregates(readModelContests);
        var oldContestAggregates = await GetAggregates<ContestAggregate>(oldContestIds);

        if (isCreate)
        {
            contestAggregate.CreateFrom(contest);
        }
        else
        {
            contestAggregate.UpdateFrom(contest);
        }

        contestAggregate.MergeContests(oldContestIds);

        foreach (var contestChildAggregate in contestChildAggregates)
        {
            contestChildAggregate.MoveToNewContest(contest.Id);
        }

        foreach (var oldContestAggregate in oldContestAggregates)
        {
            oldContestAggregate.Delete();
        }

        await _aggregateRepository.Save(contestAggregate);

        foreach (var contestChildAggregate in contestChildAggregates)
        {
            try
            {
                await _aggregateRepository.Save(contestChildAggregate);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Could not move contest child aggregate {AggregateId} to new contest {NewContestId}",
                    contestChildAggregate.Id,
                    contest.Id);
            }
        }

        foreach (var oldContestAggregate in oldContestAggregates)
        {
            try
            {
                await _aggregateRepository.Save(oldContestAggregate);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Could not delete contest aggregate {AggregateId} during the merge to {NewContestId}",
                    oldContestAggregate.Id,
                    contest.Id);
            }
        }
    }

    private async Task<IReadOnlyCollection<Data.Models.Contest>> LoadReadModelContestsWithChilds(IReadOnlyCollection<Guid> contestIds)
    {
        return await _contestRepo.Query()
            .AsSplitQuery()
            .Include(c => c.Votes)
            .Include(c => c.ProportionalElections)
            .Include(c => c.MajorityElections)
            .Include(c => c.ProportionalElectionUnions)
            .Include(c => c.MajorityElectionUnions)
            .Where(c => contestIds.Contains(c.Id))
            .ToListAsync();
    }

    private async Task<IReadOnlyCollection<BaseHasContestAggregate>> GetContestChildAggregates(IReadOnlyCollection<Data.Models.Contest> readModelContests)
    {
        return (
                await GetContestChildAggregates<VoteAggregate>(
                    readModelContests.SelectMany(c => c.Votes.Select(v => v.Id)).ToList()))
            .Concat(
                await GetContestChildAggregates<ProportionalElectionAggregate>(
                    readModelContests.SelectMany(c => c.ProportionalElections.Select(pe => pe.Id)).ToList()))
            .Concat(
                await GetContestChildAggregates<MajorityElectionAggregate>(
                    readModelContests.SelectMany(c => c.MajorityElections.Select(me => me.Id)).ToList()))
            .Concat(
                await GetContestChildAggregates<ProportionalElectionUnionAggregate>(
                    readModelContests.SelectMany(c => c.ProportionalElectionUnions.Select(peu => peu.Id)).ToList()))
            .Concat(
                await GetContestChildAggregates<MajorityElectionUnionAggregate>(
                    readModelContests.SelectMany(c => c.MajorityElectionUnions.Select(meu => meu.Id)).ToList()))
            .ToList();
    }

    private async Task<IReadOnlyCollection<BaseHasContestAggregate>> GetContestChildAggregates<TAggregate>(IReadOnlyCollection<Guid> ids)
        where TAggregate : BaseHasContestAggregate
    {
        return await GetAggregates<TAggregate>(ids);
    }

    private async Task<IReadOnlyCollection<TAggregate>> GetAggregates<TAggregate>(IReadOnlyCollection<Guid> ids)
        where TAggregate : BaseDeletableAggregate
    {
        if (ids.Distinct().Count() != ids.Count)
        {
            throw new ValidationException("duplicate ids present");
        }

        var aggregates = new List<TAggregate>();
        foreach (var id in ids)
        {
            var aggregate = await _aggregateRepository.GetById<TAggregate>(id);
            aggregates.Add(aggregate);
        }

        return aggregates;
    }
}
