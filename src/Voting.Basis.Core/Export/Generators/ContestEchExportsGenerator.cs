// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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

public class ContestEchExportsGenerator : ContestEchExportsGeneratorBase
{
    public ContestEchExportsGenerator(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService,
        CountingCircleReader countingCircleReader)
        : base(auth, contestRepo, echSerializerProvider, permissionService, countingCircleReader)
    {
    }

    public override TemplateModel Template => BasisXmlContestTemplates.Ech0157And0159;
}
