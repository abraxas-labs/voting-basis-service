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

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionListTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionListTest(TestApplicationFactory factory)
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
        var response = await ElectionAdminClient.ListAsync(new ListMajorityElectionRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task NoElectionsShouldReturnOk()
    {
        var response = await ElectionAdminClient.ListAsync(new ListMajorityElectionRequest
        {
            ContestId = ContestMockedData.IdUzwilEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestForeignContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ListAsync(new ListMajorityElectionRequest
            {
                ContestId = ContestMockedData.IdKirche,
            }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .ListAsync(new ListMajorityElectionRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
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
