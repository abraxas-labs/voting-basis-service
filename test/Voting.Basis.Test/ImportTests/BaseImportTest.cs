// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Net;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Voting.Basis.Controllers.Models;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.ImportTests.TestFiles;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;

namespace Voting.Basis.Test.ImportTests;

public abstract class BaseImportTest : BaseGrpcTest<ImportService.ImportServiceClient>
{
    private const string Url = "api/imports";

    protected BaseImportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    protected async Task<ContestImport> LoadContestImport(ImportType importType, string filePath)
    {
        var httpClient = CreateHttpClient(true, SecureConnectTestDefaults.MockedTenantDefault.Id, roles: Roles.CantonAdmin);

        var response = await HttpUtil.RequestWithFileAndData(
            filePath,
            new ResolveImportFileRequest
            {
                ImportType = importType,
            },
            async content => await httpClient.PostAsync(Url, content));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseByteArray = await response.Content.ReadAsByteArrayAsync();
        return ContestImport.Parser.ParseFrom(responseByteArray);
    }

    protected Task<string> GetTestEch0157File() => ReadTestFile(EchTestFiles.Ech0157FileName);

    protected Task<string> GetTestEch0159File() => ReadTestFile(EchTestFiles.Ech0159FileName);

    protected Task<string> GetTestEch0159AllTypesFile() => ReadTestFile(EchTestFiles.Ech0159AllTypesFileName);

    protected Task<string> GetTestInvalidEch0159File() => ReadTestFile(EchTestFiles.Ech0159InvalidFileName);

    private async Task<string> ReadTestFile(string fileName)
    {
        var path = EchTestFiles.GetTestFilePath(fileName);
        return await File.ReadAllTextAsync(path);
    }
}
