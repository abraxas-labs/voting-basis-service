// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Services.Write;

public abstract class PoliticalBusinessWriter
{
    private readonly IDbRepository<DataContext, DomainOfInfluence> _domainOfInfluenceRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;

    protected PoliticalBusinessWriter(
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        IDbRepository<DataContext, Contest> contestRepo)
    {
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _contestRepo = contestRepo;
    }

    public abstract PoliticalBusinessType Type { get; }

    /// <summary>
    /// Delete the political businesses.
    /// Should not perform a permission check.
    /// </summary>
    /// <param name="ids">The ids to delete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal abstract Task DeleteWithoutChecks(List<Guid> ids);

    protected async Task HandleEVotingDuringCreation(Domain.PoliticalBusiness politicalBusiness)
    {
        var contestEVotingInfo = await _contestRepo
            .Query()
            .Where(c => c.Id == politicalBusiness.ContestId)
            .Select(c => new
            {
                HasEVoting = c.EVoting,
                ContestIsApproved = c.EVotingApproved,
            })
            .FirstOrDefaultAsync();
        if (contestEVotingInfo?.HasEVoting != true)
        {
            return;
        }

        var hasEVotingOnDomainOfInfluence = await _domainOfInfluenceRepo
            .Query()
            .AnyAsync(d => d.Id == politicalBusiness.DomainOfInfluenceId && d.CountingCircles.Any(cc => cc.CountingCircle.EVoting));
        if (!hasEVotingOnDomainOfInfluence)
        {
            return;
        }

        if (contestEVotingInfo.ContestIsApproved)
        {
            throw new ValidationException("Cannot create a new e-voting political business when the contest e-voting approval has been set.");
        }
        else
        {
            // Deactivates e-voting on political businesses, according jira-6249.
            politicalBusiness.EVotingApproved = null;

            // To reactivate e-voting on political businesses, uncomment the following line.
            // politicalBusiness.EVotingApproved = false;
        }
    }
}
