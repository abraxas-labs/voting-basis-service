// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Basis.Ech.Converters.V5_2;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators.Xml.V5_2;

public class ContestEchOnlyEVotingExportsGenerator : ContestEchOnlyEVotingExportsGeneratorBase
{
    private readonly Ech0157Serializer _ech0157Serializer;
    private readonly Ech0159Serializer _ech0159Serializer;

    public ContestEchOnlyEVotingExportsGenerator(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        PermissionService permissionService,
        Ech0157Serializer ech0157Serializer,
        Ech0159Serializer ech0159Serializer)
        : base(auth, contestRepo, permissionService)
    {
        _ech0157Serializer = ech0157Serializer;
        _ech0159Serializer = ech0159Serializer;
    }

    public override TemplateModel Template => BasisXmlContestTemplates.Ech0157And0159_5_2_EVotingOnly;

    protected override IEch0157Serializer Ech0157Serializer => _ech0157Serializer;

    protected override IEch0159Serializer Ech0159Serializer => _ech0159Serializer;
}
