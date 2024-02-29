// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListTest(TestApplicationFactory factory)
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
        var response = await ElectionAdminClient.ListAsync(new ListProportionalElectionRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task NoElectionsShouldReturnOk()
    {
        var response = await ElectionAdminClient.ListAsync(new ListProportionalElectionRequest
        {
            ContestId = ContestMockedData.IdUzwilEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestForeignContestShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ListAsync(new ListProportionalElectionRequest
            {
                ContestId = ContestMockedData.IdKirche,
            }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .ListAsync(new ListProportionalElectionRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
