// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Basis.Ech.Converters.V5_1;
using Voting.Lib.Database.Repositories;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators.Xml.V5_1;

public class MajorityElectionEchExportGenerator : MajorityElectionEchExportGeneratorBase
{
    private readonly IEch0157Serializer _ech0157Serializer;

    public MajorityElectionEchExportGenerator(
        IDbRepository<DataContext, MajorityElection> electionRepo,
        Ech0157Serializer ech0157Serializer,
        PermissionService permissionService)
        : base(electionRepo, permissionService)
    {
        _ech0157Serializer = ech0157Serializer;
    }

    public override TemplateModel Template => BasisXmlMajorityElectionTemplates.Ech0157_5_1;

    protected override IEch0157Serializer Ech0157Serializer => _ech0157Serializer;
}
