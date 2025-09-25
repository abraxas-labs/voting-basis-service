// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators;

public class ContestEchOnlyEVotingExportsGenerator : ContestEchExportsGeneratorBase
{
    public ContestEchOnlyEVotingExportsGenerator(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService,
        CountingCircleReader countingCircleReader)
        : base(auth, contestRepo, echSerializerProvider, permissionService, countingCircleReader)
    {
    }

    public override TemplateModel Template => BasisXmlContestTemplates.Ech0157And0159EVotingOnly;

    protected override IQueryable<Contest> FilterPoliticalBusinesses(IQueryable<Contest> baseQuery)
    {
        // EVotingApproved is not null if the political business has e-voting enabled
        return baseQuery
            .Include(c => c.Votes.Where(x => x.EVotingApproved != null))
            .Include(c => c.ProportionalElections.Where(x => x.EVotingApproved != null))
            .Include(c => c.MajorityElections.Where(x => x.EVotingApproved != null));
    }
}
