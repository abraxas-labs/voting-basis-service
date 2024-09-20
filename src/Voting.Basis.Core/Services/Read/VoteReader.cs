// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class VoteReader : PoliticalBusinessReader<Vote>
{
    public VoteReader(
        IDbRepository<DataContext, Vote> repo,
        IAuth auth,
        PermissionService permissionService)
        : base(auth, permissionService, repo)
    {
    }

    protected override async Task<Vote> QueryById(Guid id)
    {
        var vote = await Repo.Query()
                    .AsSplitQuery()
                    .Include(v => v.DomainOfInfluence)
                    .Include(v => v.Ballots)
                    .ThenInclude(b => b.BallotQuestions)
                    .Include(v => v.Ballots)
                    .ThenInclude(b => b.TieBreakQuestions)
                    .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new EntityNotFoundException(id);

        // order by can be included with includes with ef core 5
        vote.Ballots = vote.Ballots
            .OrderBy(b => b.Position)
            .ToList();

        foreach (var ballot in vote.Ballots)
        {
            ballot.BallotQuestions = ballot.BallotQuestions
                .OrderBy(b => b.Number)
                .ToList();

            ballot.TieBreakQuestions = ballot.TieBreakQuestions
                .OrderBy(tb => tb.Number)
                .ToList();
        }

        return vote;
    }
}
