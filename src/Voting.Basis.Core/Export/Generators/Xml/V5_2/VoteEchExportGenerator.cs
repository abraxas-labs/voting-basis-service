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

public class VoteEchExportGenerator : VoteEchExportGeneratorBase
{
    private readonly Ech0159Serializer _ech0159Serializer;

    public VoteEchExportGenerator(
        IDbRepository<DataContext, Vote> voteRepo,
        PermissionService permissionService,
        Ech0159Serializer ech0159Serializer)
        : base(voteRepo, permissionService)
    {
        _ech0159Serializer = ech0159Serializer;
    }

    public override TemplateModel Template => BasisXmlVoteTemplates.Ech0159_5_2;

    protected override IEch0159Serializer Ech0159Serializer => _ech0159Serializer;
}
