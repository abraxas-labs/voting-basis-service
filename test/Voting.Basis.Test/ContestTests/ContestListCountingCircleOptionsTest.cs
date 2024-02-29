// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Snapper;
using Voting.Basis.Test.MockedData;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestListCountingCircleOptionsTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestListCountingCircleOptionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldWork()
    {
        var resp = await AdminClient.ListCountingCircleOptionsAsync(new ListCountingCircleOptionsRequest
        {
            Id = ContestMockedData.IdStGallenEvoting,
        });
        resp.ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdmin()
    {
        var resp = await ElectionAdminClient.ListCountingCircleOptionsAsync(new ListCountingCircleOptionsRequest
        {
            Id = ContestMockedData.IdStGallenEvoting,
        });
        resp.Options.Should().HaveCount(4);
        resp.Options.Single(x => x.CountingCircle.Id == CountingCircleMockedData.IdGossau).EVoting.Should().BeTrue();
        resp.Options
            .Where(x => x.CountingCircle.Id != CountingCircleMockedData.IdGossau)
            .All(x => !x.EVoting)
            .Should()
            .BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ListCountingCircleOptionsAsync(new ListCountingCircleOptionsRequest
            {
                Id = ContestMockedData.IdStGallenEvoting,
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
