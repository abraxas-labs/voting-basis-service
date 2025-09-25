// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
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
    public async Task TestAsCantonAdminShouldWork()
    {
        var response = await CantonAdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsCantonAdminReadOnlyShouldWork()
    {
        var response = await CantonAdminReadOnlyClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldWork()
    {
        var response = await ElectionAdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminEVotingAdminShouldWork()
    {
        var response = await ElectionAdminEVotingAdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminReadOnlyShouldWork()
    {
        var response = await ElectionAdminReadOnlyClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionSupporterShouldWork()
    {
        var response = await ElectionSupporterClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsApiReaderShouldWork()
    {
        var response = await ApiReaderClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    protected override IEnumerable<string> AuthorizedRoles()
        => Roles.All();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield break;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new PermissionService.PermissionServiceClient(channel)
            .ListAsync(new Empty());
    }
}
