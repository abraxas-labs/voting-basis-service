// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleListTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    public CountingCircleListTest(TestApplicationFactory factory)
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
        var list = await ElectionAdminClient.ListAsync(new ListCountingCircleRequest());
        list.CountingCircles_.Should().HaveCount(4);
        list.CountingCircles_.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAll()
    {
        var list = await AdminClient.ListAsync(new ListCountingCircleRequest());
        list.CountingCircles_.Should().HaveCount(8);
        list.CountingCircles_.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .ListAsync(new ListCountingCircleRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
