// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Basis.Ech.Converters.V5_2;
using Voting.Lib.Database.Repositories;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators.Xml.V5_2;

public class ProportionalElectionEchExportGenerator : ProportionalElectionEchExportGeneratorBase
{
    private readonly IEch0157Serializer _ech0157Serializer;

    public ProportionalElectionEchExportGenerator(
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        PermissionService permissionService,
        Ech0157Serializer ech0157Serializer)
        : base(electionRepo, permissionService)
    {
        _ech0157Serializer = ech0157Serializer;
    }

    public override TemplateModel Template => BasisXmlProportionalElectionTemplates.Ech0157_5_2;

    protected override IEch0157Serializer Ech0157Serializer => _ech0157Serializer;
}
