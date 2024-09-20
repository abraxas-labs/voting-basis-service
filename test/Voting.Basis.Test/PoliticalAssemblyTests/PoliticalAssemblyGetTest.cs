// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.PoliticalAssemblyTests;

public class PoliticalAssemblyGetTest : BaseGrpcTest<PoliticalAssemblyService.PoliticalAssemblyServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public PoliticalAssemblyGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await PoliticalAssemblyMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AdminClient.GetAsync(new GetPoliticalAssemblyRequest
        {
            Id = PoliticalAssemblyMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetAsync(new GetPoliticalAssemblyRequest
        {
            Id = PoliticalAssemblyMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetAsync(new GetPoliticalAssemblyRequest
            {
                Id = PoliticalAssemblyMockedData.IdKirche,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestInvalidGuidShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.GetAsync(new GetPoliticalAssemblyRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.GetAsync(new GetPoliticalAssemblyRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new PoliticalAssemblyService.PoliticalAssemblyServiceClient(channel)
            .GetAsync(new GetPoliticalAssemblyRequest
            {
                Id = PoliticalAssemblyMockedData.IdGossau,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
