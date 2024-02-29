// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.CantonSettingsTests;

public class CantonSettingsListTest : BaseGrpcTest<CantonSettingsService.CantonSettingsServiceClient>
{
    public CantonSettingsListTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsElectionAdminShouldListAllWithMatchingAuthority()
    {
        var list = await ElectionAdminClient.ListAsync(new ListCantonSettingsRequest());
        list.CantonSettingsList_.Should().HaveCount(1);
        list.CantonSettingsList_.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAll()
    {
        var list = await AdminClient.ListAsync(new ListCantonSettingsRequest());
        list.CantonSettingsList_.Should().HaveCount(2);
        list.CantonSettingsList_.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CantonSettingsService.CantonSettingsServiceClient(channel)
            .ListAsync(new ListCantonSettingsRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
