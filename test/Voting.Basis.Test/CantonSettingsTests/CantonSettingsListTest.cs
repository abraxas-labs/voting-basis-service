// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
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
    public async Task TestAsCantonAdminShouldListAllWithMatchingAuthority()
    {
        var list = await CantonAdminClient.ListAsync(new ListCantonSettingsRequest());
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
