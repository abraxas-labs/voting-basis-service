// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Basis.Core.Utils;

public class ContestDeleter
{
    private readonly ILogger<ContestDeleter> _logger;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IEnumerable<PoliticalBusinessUnionWriter> _politicalBusinessUnionWriters;
    private readonly PoliticalBusinessDeleter _politicalBusinessDeleter;

    public ContestDeleter(
        ILogger<ContestDeleter> logger,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, Contest> contestRepo,
        IEnumerable<PoliticalBusinessUnionWriter> politicalBusinessUnionWriters,
        PoliticalBusinessDeleter politicalBusinessDeleter)
    {
        _logger = logger;
        _aggregateRepository = aggregateRepository;
        _contestRepo = contestRepo;
        _politicalBusinessUnionWriters = politicalBusinessUnionWriters;
        _politicalBusinessDeleter = politicalBusinessDeleter;
    }

    internal async Task Delete(ContestAggregate aggregate)
    {
        await DeleteRelatedEntities([aggregate.Id]);
        await DeleteContestAggregate(aggregate.Id, aggregate);
    }

    internal async Task DeleteInTestingPhase(List<Guid> domainOfInfluenceIds)
    {
        var idsToDelete = await _contestRepo.Query()
            .Where(x => x.State == ContestState.TestingPhase && domainOfInfluenceIds.Contains(x.DomainOfInfluenceId))
            .Select(x => x.Id)
            .ToListAsync();
        await DeleteRelatedEntities(idsToDelete);

        foreach (var id in idsToDelete)
        {
            await DeleteContestAggregate(id);
        }
    }

    private async Task DeleteRelatedEntities(List<Guid> contestIds)
    {
        foreach (var unionWriter in _politicalBusinessUnionWriters)
        {
            await unionWriter.DeleteForContests(contestIds);
        }

        await _politicalBusinessDeleter.DeleteForContestsInTestingPhase(contestIds);
    }

    private async Task DeleteContestAggregate(Guid id, ContestAggregate? aggregate = null)
    {
        aggregate ??= await _aggregateRepository.GetById<ContestAggregate>(id);
        aggregate.Delete();
        await _aggregateRepository.Save(aggregate);
        _logger.LogInformation("Deleted contest {ContestId}.", id);
    }
}
