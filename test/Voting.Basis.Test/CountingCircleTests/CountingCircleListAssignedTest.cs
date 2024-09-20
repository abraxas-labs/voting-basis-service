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

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleListAssignedTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    public CountingCircleListAssignedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAllCountingCirclesWithMatchingDomainOfInfluenceId()
    {
        var list = await AdminClient.ListAssignedAsync(new ListAssignedCountingCircleRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
        });
        list.CountingCircles.Should().HaveCount(5);
        list.CountingCircles.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnEmptyWithNotAccessibleMatchingDomainOfInfluenceId()
    {
        var list = await ElectionAdminClient.ListAssignedAsync(new ListAssignedCountingCircleRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdThurgau,
        });
        list.CountingCircles.Should().HaveCount(0);
        list.CountingCircles.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnAllAccessibleCountingCirclesWithMatchingDomainOfInfluenceId()
    {
        var list = await ElectionAdminClient.ListAssignedAsync(new ListAssignedCountingCircleRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        list.CountingCircles.Should().HaveCount(4);
        list.CountingCircles.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .ListAsync(new ListCountingCircleRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
