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

public class DomainOfInfluenceGetTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturn()
    {
        var response = await AdminClient.GetAsync(new GetDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturn()
    {
        var response = await ElectionAdminClient.GetAsync(new GetDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantAsAdminShouldReturn()
    {
        var response = await AdminClient.GetAsync(new GetDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdUzwil,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public Task TestOtherTenantAsElectionAdmin()
        => AssertStatus(
            async () => await ElectionAdminClient.GetAsync(new GetDomainOfInfluenceRequest
            {
                Id = DomainOfInfluenceMockedData.IdKirchgemeinde,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task TestInvalidGuid()
        => await AssertStatus(
            async () => await AdminClient.GetAsync(new GetDomainOfInfluenceRequest
            {
                Id = DomainOfInfluenceMockedData.IdInvalid,
            }),
            StatusCode.InvalidArgument);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .GetAsync(new GetDomainOfInfluenceRequest
            {
                Id = DomainOfInfluenceMockedData.IdBund,
            });

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
