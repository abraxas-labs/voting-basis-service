// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.AdminManagementTests;

public class GetPoliticalDoiHierarchyTest : BaseGrpcTest<AdminManagementService.AdminManagementServiceClient>
{
    public GetPoliticalDoiHierarchyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsApiReaderShouldReturnAll()
    {
        var response = await ApiReaderClient.GetPoliticalDomainOfInfluenceHierarchyAsync(new());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsApiReaderEmptyShouldReturnEmpty()
    {
        await RunOnDb(async db =>
        {
            db.DomainOfInfluences.RemoveRange(db.DomainOfInfluences);
            await db.SaveChangesAsync();
        });
        var response = await ApiReaderClient.GetPoliticalDomainOfInfluenceHierarchyAsync(new());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsApiReaderDefaultTenantShouldReturnAll()
    {
        var response = await ApiReaderClient.GetPoliticalDomainOfInfluenceHierarchyAsync(new());
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new AdminManagementService.AdminManagementServiceClient(channel)
            .GetPoliticalDomainOfInfluenceHierarchyAsync(new());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.ElectionAdmin;
    }
}
