// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class ContestCountingCircleOptionsRepo : DbRepository<DataContext, ContestCountingCircleOption>
{
    public ContestCountingCircleOptionsRepo(DataContext context)
        : base(context)
    {
    }

    public async Task Replace(IEnumerable<Guid> contestIds, IEnumerable<ContestCountingCircleOption> newOptions)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        await Context.Database.ExecuteSqlRawAsync(
            $"DELETE FROM {DelimitedSchemaAndTableName} WHERE {contestIdColName} = ANY({{0}})",
            contestIds.ToArray());

        Context.ContestCountingCircleOptions.AddRange(newOptions);
        await Context.SaveChangesAsync();
    }

    public async Task Replace(Guid contestId, IEnumerable<ContestCountingCircleOption> newOptions)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        await Context.Database.ExecuteSqlRawAsync(
            $"DELETE FROM {DelimitedSchemaAndTableName} WHERE {contestIdColName} = {{0}}",
            contestId);

        Context.ContestCountingCircleOptions.AddRange(newOptions);
        await Context.SaveChangesAsync();
    }
}
