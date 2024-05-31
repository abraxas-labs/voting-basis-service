// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.ProportionalElectionUnionTests;

public class ProportionalElectionUnionGetProportionalElectionUnionListsTest : BaseGrpcTest<ProportionalElectionUnionService.ProportionalElectionUnionServiceClient>
{
    public ProportionalElectionUnionGetProportionalElectionUnionListsTest(TestApplicationFactory factory)
       : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetProportionalElectionUnionListsAsync(new GetProportionalElectionUnionListsRequest
        {
            ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task DifferentTenantButAccessOnSameContestShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetProportionalElectionUnionListsAsync(new GetProportionalElectionUnionListsRequest
        {
            ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdBundDifferentTenant,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task NoAccessOnContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetProportionalElectionUnionListsAsync(
                new GetProportionalElectionUnionListsRequest
                {
                    ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdKirche,
                }),
            StatusCode.PermissionDenied);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionService.ProportionalElectionUnionServiceClient(channel)
            .GetProportionalElectionUnionListsAsync(new GetProportionalElectionUnionListsRequest
            {
                ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
            });
    }
}
