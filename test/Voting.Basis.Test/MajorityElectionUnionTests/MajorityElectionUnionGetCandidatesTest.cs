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

namespace Voting.Basis.Test.MajorityElectionUnionTests;

public class MajorityElectionUnionGetCandidatesTest : BaseGrpcTest<MajorityElectionUnionService.MajorityElectionUnionServiceClient>
{
    public MajorityElectionUnionGetCandidatesTest(TestApplicationFactory factory)
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
        var response = await ElectionAdminClient.GetCandidatesAsync(new GetMajorityElectionUnionCandidatesRequest
        {
            MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task DifferentTenantButAccessOnSameContestShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetCandidatesAsync(new GetMajorityElectionUnionCandidatesRequest
        {
            MajorityElectionUnionId = MajorityElectionUnionMockedData.IdBundDifferentTenant,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task NoAccessOnContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetCandidatesAsync(
                new GetMajorityElectionUnionCandidatesRequest
                {
                    MajorityElectionUnionId = MajorityElectionUnionMockedData.IdKirche,
                }),
            StatusCode.PermissionDenied);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionUnionService.MajorityElectionUnionServiceClient(channel)
            .GetCandidatesAsync(new GetMajorityElectionUnionCandidatesRequest
            {
                MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
            });
    }
}
