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
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Utils;

public class PoliticalBusinessEVotingApprovalInitializer
{
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _domainOfInfluenceRepo;

    public PoliticalBusinessEVotingApprovalInitializer(IDbRepository<DataContext, Contest> contestRepo, IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo)
    {
        _contestRepo = contestRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
    }

    public async Task Initialize(List<Domain.PoliticalBusiness> politicalBusinesses, Guid? contestId = null)
    {
        if (politicalBusinesses.Count == 0)
        {
            return;
        }

        contestId ??= politicalBusinesses.Select(pb => pb.ContestId).First();

        var contestEVotingInfo = await _contestRepo
            .Query()
            .Where(c => c.Id == contestId)
            .Select(c => new ContestEVotingInfo
            {
                HasEVoting = c.EVoting,
                ContestIsApproved = c.EVotingApproved,
            })
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(contestId);

        await Initialize(politicalBusinesses, contestEVotingInfo);
    }

    public async Task InitializeForNewContest(List<Domain.PoliticalBusiness> politicalBusinesses, bool contestHasEVoting)
    {
        if (politicalBusinesses.Count == 0)
        {
            return;
        }

        var contestEVotingInfo = new ContestEVotingInfo { HasEVoting = contestHasEVoting, ContestIsApproved = false };
        await Initialize(politicalBusinesses, contestEVotingInfo);
    }

    private async Task Initialize(List<Domain.PoliticalBusiness> politicalBusinesses, ContestEVotingInfo contestEVotingInfo)
    {
        if (!contestEVotingInfo.HasEVoting)
        {
            return;
        }

        var domainOfInfluenceIds = politicalBusinesses.ConvertAll(pb => pb.DomainOfInfluenceId);

        var eVotingOnDomainOfInfluenceIds = await _domainOfInfluenceRepo
            .Query()
            .Where(d => domainOfInfluenceIds.Contains(d.Id) && d.CountingCircles.Any(cc => cc.CountingCircle.EVoting))
            .Select(d => d.Id)
            .ToListAsync();

        foreach (var politicalBusiness in politicalBusinesses)
        {
            if (!eVotingOnDomainOfInfluenceIds.Contains(politicalBusiness.DomainOfInfluenceId))
            {
                continue;
            }

            if (contestEVotingInfo.ContestIsApproved)
            {
                throw new ValidationException("Cannot create a new e-voting political business when the contest e-voting approval has been set.");
            }
            else
            {
                // Deactivatess e-voting on political businesses,
                politicalBusiness.EVotingApproved = null;

                // To reactivate e-voting on political businesses, uncomment the following line.
                // politicalBusiness.EVotingApproved = false;
            }
        }
    }

    private sealed class ContestEVotingInfo
    {
        public bool HasEVoting { get; set; }

        public bool ContestIsApproved { get; set; }
    }
}
