﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class BallotQuestionRepo : DbRepository<DataContext, BallotQuestion>
{
    public BallotQuestionRepo(DataContext context)
        : base(context)
    {
    }

    public async Task Replace(Guid ballotId, IEnumerable<BallotQuestion> entries)
    {
        var existingEntries = await Set.Where(e => e.BallotId == ballotId).ToArrayAsync();

        Set.RemoveRange(existingEntries);
        await Context.SaveChangesAsync();

        Set.AddRange(entries);
        await Context.SaveChangesAsync();
    }
}
