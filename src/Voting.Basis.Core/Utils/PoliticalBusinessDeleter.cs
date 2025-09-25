// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;

namespace Voting.Basis.Core.Utils;

public class PoliticalBusinessDeleter
{
    private readonly IEnumerable<PoliticalBusinessWriter> _politicalBusinessWriters;
    private readonly SimplePoliticalBusinessRepo _simplePoliticalBusinessRepo;

    public PoliticalBusinessDeleter(
        IEnumerable<PoliticalBusinessWriter> politicalBusinessWriters,
        SimplePoliticalBusinessRepo simplePoliticalBusinessRepo)
    {
        _politicalBusinessWriters = politicalBusinessWriters.OrderBy(x => x.Type);
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    internal async Task DeleteForContestsInTestingPhase(List<Guid> contestIds)
    {
        var toDeleteByType = await _simplePoliticalBusinessRepo.Query()
            .Where(x => contestIds.Contains(x.ContestId) && x.BusinessType != PoliticalBusinessType.SecondaryMajorityElection)
            .Select(x => new
            {
                x.Id,
                x.BusinessType,
            })
            .GroupBy(x => x.BusinessType)
            .ToDictionaryAsync(x => x.Key, x => x.Select(y => y.Id).ToList());
        await DeletePoliticalBusinesses(toDeleteByType);
    }

    internal async Task DeleteForDomainOfInfluencesInTestingPhase(List<Guid> doiIds)
    {
        // This does NOT delete political business of contests that are owned by one of the DOIs,
        // as those should be deleted in a separate step (since the whole contest will be deleted).
        var toDeleteByType = await _simplePoliticalBusinessRepo.Query()
            .Where(x => x.Contest.State == ContestState.TestingPhase
                && !doiIds.Contains(x.Contest.DomainOfInfluenceId)
                && doiIds.Contains(x.DomainOfInfluenceId)
                && x.BusinessType != PoliticalBusinessType.SecondaryMajorityElection)
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.BusinessType,
            })
            .GroupBy(x => x.BusinessType)
            .ToDictionaryAsync(x => x.Key, x => x.Select(y => y.Id).ToList());
        await DeletePoliticalBusinesses(toDeleteByType);
    }

    private async Task DeletePoliticalBusinesses(Dictionary<PoliticalBusinessType, List<Guid>> idsByBusinessType)
    {
        foreach (var politicalBusinessWriter in _politicalBusinessWriters)
        {
            if (idsByBusinessType.TryGetValue(politicalBusinessWriter.Type, out var ids))
            {
                await politicalBusinessWriter.DeleteWithoutChecks(ids);
            }
        }
    }
}
