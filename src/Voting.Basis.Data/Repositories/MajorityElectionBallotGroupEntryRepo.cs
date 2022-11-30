// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class MajorityElectionBallotGroupEntryRepo : DbRepository<DataContext, MajorityElectionBallotGroupEntry>
{
    public MajorityElectionBallotGroupEntryRepo(DataContext context)
        : base(context)
    {
    }

    public async Task UpdateCandidateCountOk(Guid electionId, bool isPrimaryElection, int numberOfMandates)
    {
        var candidateCountOkColName = GetDelimitedColumnName(x => x.CandidateCountOk);
        var individualCountColName = GetDelimitedColumnName(x => x.IndividualCandidatesVoteCount);
        var blankRowCountColName = GetDelimitedColumnName(x => x.BlankRowCount);
        var countOfCandidatesColName = GetDelimitedColumnName(x => x.CountOfCandidates);
        var electionIdColName = isPrimaryElection
            ? GetDelimitedColumnName(x => x.PrimaryMajorityElectionId)
            : GetDelimitedColumnName(x => x.SecondaryMajorityElectionId);
        await Context.Database.ExecuteSqlRawAsync(
            $"UPDATE {DelimitedSchemaAndTableName} " +
            $"SET {candidateCountOkColName} = ({{0}} = {blankRowCountColName} + {countOfCandidatesColName} + {individualCountColName}) " +
            $"WHERE {electionIdColName} = {{1}}",
            numberOfMandates,
            electionId);
    }
}
