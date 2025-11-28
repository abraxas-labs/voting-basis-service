// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Voting.Basis.Core.Export.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Database.Repositories;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators.Xml.V5;

public class ProportionalElectionEchExportGenerator : ProportionalElectionEchExportGeneratorBase, IExportGenerator
{
    public ProportionalElectionEchExportGenerator(
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService)
        : base(electionRepo, echSerializerProvider, permissionService)
    {
    }

    public TemplateModel Template => BasisXmlProportionalElectionTemplates.Ech0157_5_1;

    public Task<ExportFile> GenerateExport(Guid entityId)
    {
        return GenerateExport(entityId, true);
    }
}
