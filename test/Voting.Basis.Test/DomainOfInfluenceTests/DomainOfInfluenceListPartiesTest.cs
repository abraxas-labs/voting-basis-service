// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceListPartiesTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceListPartiesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsElectionAdmin()
    {
        var response = await ElectionAdminClient.ListPartiesAsync(NewRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdmin()
    {
        var response = await AdminClient.ListPartiesAsync(NewRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await ElectionAdminClient.ListPartiesAsync(new ListDomainOfInfluencePartiesRequest
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdKirchgemeindeAndere,
            }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .ListPartiesAsync(NewRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private ListDomainOfInfluencePartiesRequest NewRequest()
    {
        return new ListDomainOfInfluencePartiesRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
        };
    }
}
