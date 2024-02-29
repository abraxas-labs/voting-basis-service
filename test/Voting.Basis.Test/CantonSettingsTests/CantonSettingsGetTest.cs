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

namespace Voting.Basis.Test.CantonSettingsTests;

public class CantonSettingsGetTest : BaseGrpcTest<CantonSettingsService.CantonSettingsServiceClient>
{
    public CantonSettingsGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturn()
    {
        var response = await ElectionAdminClient.GetAsync(new GetCantonSettingsRequest { Id = CantonSettingsMockedData.IdZurich });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantAsElectionAdminShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetAsync(new GetCantonSettingsRequest { Id = CantonSettingsMockedData.IdStGallen }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestAsAdminShouldReturn()
    {
        var response = await AdminClient.GetAsync(new GetCantonSettingsRequest { Id = CantonSettingsMockedData.IdZurich });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CantonSettingsService.CantonSettingsServiceClient(channel)
            .GetAsync(new GetCantonSettingsRequest { Id = CantonSettingsMockedData.IdStGallen });

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
