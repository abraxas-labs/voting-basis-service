// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class SimplePoliticalBusinessRepo : DbRepository<DataContext, SimplePoliticalBusiness>
{
    public SimplePoliticalBusinessRepo(DataContext context)
        : base(context)
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task MoveToNewContest(Guid oldContestId, Guid newContestId)
    {
        var contestIdColName = GetDelimitedColumnName(x => x.ContestId);
        await Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} SET {contestIdColName} = {{0}} WHERE {contestIdColName} = {{1}}",
            newContestId,
            oldContestId);
    }
}
