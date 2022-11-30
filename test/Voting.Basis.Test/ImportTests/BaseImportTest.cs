// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Voting.Basis.Test.MockedData;

namespace Voting.Basis.Test.ImportTests;

public abstract class BaseImportTest : BaseGrpcTest<ImportService.ImportServiceClient>
{
    protected BaseImportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    protected Task<string> GetTestEch157File() => ReadTestFile("eCH157_both_election_types.xml");

    protected Task<string> GetTestEch159File() => ReadTestFile("eCH159.xml");

    private async Task<string> ReadTestFile(string fileName)
    {
        var assemblyFolder = Path.GetDirectoryName(GetType().Assembly.Location);
        var path = Path.Join(assemblyFolder, $"ImportTests/TestFiles/{fileName}");
        return await File.ReadAllTextAsync(path);
    }
}
