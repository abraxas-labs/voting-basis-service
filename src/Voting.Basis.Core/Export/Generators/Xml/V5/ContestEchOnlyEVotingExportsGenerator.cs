// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Core.Export.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators.Xml.V5;

public class ContestEchOnlyEVotingExportsGenerator : ContestEchExportsGeneratorBase, IExportsGenerator
{
    public ContestEchOnlyEVotingExportsGenerator(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService)
        : base(auth, contestRepo, echSerializerProvider, permissionService)
    {
    }

    public TemplateModel Template => BasisXmlContestTemplates.Ech0157And0159_5_1_EVotingOnly;

    public async IAsyncEnumerable<ExportFile> GenerateExports(Guid entityId)
    {
        await foreach (var exportFile in GenerateExports(entityId, false))
        {
            yield return exportFile;
        }
    }
}
