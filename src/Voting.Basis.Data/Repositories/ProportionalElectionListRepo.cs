// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class ProportionalElectionListRepo : DbRepository<DataContext, ProportionalElectionList>
{
    public ProportionalElectionListRepo(DataContext context)
        : base(context)
    {
    }

    public async Task UpdateCandidateCountOk(Guid electionId, int numberOfMandates)
    {
        var candidateCountOkColName = GetDelimitedColumnName(x => x.CandidateCountOk);
        var blankRowCountColName = GetDelimitedColumnName(x => x.BlankRowCount);
        var countOfCandidatesColName = GetDelimitedColumnName(x => x.CountOfCandidates);
        var electionIdColName = GetDelimitedColumnName(x => x.ProportionalElectionId);
        await Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} " +
            $"SET {candidateCountOkColName} = ({{0}} = {blankRowCountColName} + {countOfCandidatesColName}) " +
            $"WHERE {electionIdColName} = {{1}}",
            numberOfMandates,
            electionId);
    }
}
