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

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceListTreeTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceListTreeTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAll()
    {
        var response = await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsCantonAdminShouldReturnSameCanton()
    {
        var response = await CantonAdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminEmptyShouldReturnEmpty()
    {
        await RunOnDb(async db =>
        {
            db.DomainOfInfluences.RemoveRange(db.DomainOfInfluences);
            await db.SaveChangesAsync();
        });
        var response = await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnAllIfAnyNodeIsAuthority()
    {
        var response = await ElectionAdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnAllIfAnyCountingCircleIsAuthority()
    {
        var client = new DomainOfInfluenceService.DomainOfInfluenceServiceClient(
            CreateGrpcChannel(
                tenant: CountingCircleMockedData.CountingCircleUzwilKircheSecureConnectId,
                roles: Roles.ElectionAdmin));
        var response = await client.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .ListTreeAsync(new ListTreeDomainOfInfluenceRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
        yield return Roles.ApiReader;
    }
}
