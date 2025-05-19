// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.PoliticalAssemblyTests;

public class PoliticalAssemblyListTest : BaseGrpcTest<PoliticalAssemblyService.PoliticalAssemblyServiceClient>
{
    public PoliticalAssemblyListTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await PoliticalAssemblyMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnActive()
    {
        var response = await ElectionAdminClient.ListAsync(new ListPoliticalAssemblyRequest { State = Abraxas.Voting.Basis.Shared.V1.PoliticalAssemblyState.Active });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnPastLocked()
    {
        var response = await ElectionAdminClient.ListAsync(new ListPoliticalAssemblyRequest { State = Abraxas.Voting.Basis.Shared.V1.PoliticalAssemblyState.PastLocked });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnArchived()
    {
        var response = await ElectionAdminClient.ListAsync(new ListPoliticalAssemblyRequest { State = Abraxas.Voting.Basis.Shared.V1.PoliticalAssemblyState.Archived });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new PoliticalAssemblyService.PoliticalAssemblyServiceClient(channel)
            .ListAsync(new ListPoliticalAssemblyRequest { State = Abraxas.Voting.Basis.Shared.V1.PoliticalAssemblyState.Active });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
    }
}
