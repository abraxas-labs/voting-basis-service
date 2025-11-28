// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Export.Generators.Xml;

public class ContestEchOnlyEVotingExportsGeneratorBase : ContestEchExportsGeneratorBase
{
    public ContestEchOnlyEVotingExportsGeneratorBase(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService)
        : base(auth, contestRepo, echSerializerProvider, permissionService)
    {
    }

    protected override IQueryable<Contest> FilterPoliticalBusinesses(IQueryable<Contest> baseQuery)
    {
        // EVotingApproved is not null if the political business has e-voting enabled
        return baseQuery
            .Include(c => c.Votes.Where(x => x.EVotingApproved != null))
            .Include(c => c.ProportionalElections.Where(x => x.EVotingApproved != null))
            .Include(c => c.MajorityElections.Where(x => x.EVotingApproved != null));
    }
}
