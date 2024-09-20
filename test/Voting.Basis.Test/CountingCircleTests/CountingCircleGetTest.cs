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

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleGetTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public CountingCircleGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsElectionAdmin()
    {
        var response = await ElectionAdminClient.GetAsync(new GetCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdUzwil,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantAsElectionAdmin()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetAsync(new GetCountingCircleRequest
            {
                Id = CountingCircleMockedData.IdUzwilKirche,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestInvalidGuid()
    {
        await AssertStatus(
            async () => await AdminClient.GetAsync(new GetCountingCircleRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.GetAsync(new GetCountingCircleRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestAsAdmin()
    {
        var response = await AdminClient.GetAsync(new GetCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdUzwil,
        });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new CountingCircleService.CountingCircleServiceClient(channel)
            .GetAsync(new GetCountingCircleRequest
            {
                Id = CountingCircleMockedData.IdStGallen,
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
