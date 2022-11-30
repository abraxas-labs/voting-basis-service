// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceGetCantonDefaultsTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceGetCantonDefaultsTest(TestApplicationFactory factory)
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
        var response = await AdminClient.GetCantonDefaultsAsync(new GetDomainOfInfluenceCantonDefaultsRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturn()
    {
        var response = await ElectionAdminClient.GetCantonDefaultsAsync(new GetDomainOfInfluenceCantonDefaultsRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestParentDoiAsElectionAdminShouldReturn()
    {
        var response = await ElectionAdminClient.GetCantonDefaultsAsync(new GetDomainOfInfluenceCantonDefaultsRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
        });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .GetCantonDefaultsAsync(new GetDomainOfInfluenceCantonDefaultsRequest
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
            });

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
