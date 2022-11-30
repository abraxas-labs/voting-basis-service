// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.MajorityElectionUnionTests;

public class MajorityElectionUnionGetPoliticalBusinessesTest : BaseGrpcTest<MajorityElectionUnionService.MajorityElectionUnionServiceClient>
{
    public MajorityElectionUnionGetPoliticalBusinessesTest(TestApplicationFactory factory)
       : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetPoliticalBusinessesAsync(
            new GetMajorityElectionUnionPoliticalBusinessesRequest
            {
                MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
            });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task DifferentTenantButAccessOnSameContestShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetPoliticalBusinessesAsync(
            new GetMajorityElectionUnionPoliticalBusinessesRequest
            {
                MajorityElectionUnionId = MajorityElectionUnionMockedData.IdBundDifferentTenant,
            });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task NoAccessOnContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetPoliticalBusinessesAsync(
                new GetMajorityElectionUnionPoliticalBusinessesRequest
                {
                    MajorityElectionUnionId = MajorityElectionUnionMockedData.IdKirche,
                }),
            StatusCode.PermissionDenied,
            "you have no read access on contest");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionUnionService.MajorityElectionUnionServiceClient(channel)
            .GetPoliticalBusinessesAsync(new GetMajorityElectionUnionPoliticalBusinessesRequest
            {
                MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
            });
    }
}
