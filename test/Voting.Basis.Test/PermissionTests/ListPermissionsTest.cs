// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.PermissionTests;

public class ListPermissionsTest : BaseGrpcTest<PermissionService.PermissionServiceClient>
{
    public ListPermissionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldWork()
    {
        var response = await AdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldWork()
    {
        var response = await ElectionAdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new PermissionService.PermissionServiceClient(channel)
            .ListAsync(new Empty());
    }
}
