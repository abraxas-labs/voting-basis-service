// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleListAssignableTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    public CountingCircleListAssignableTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminORootDoiShouldReturnOk()
    {
        var list = await AdminClient.ListAssignableAsync(new ListAssignableCountingCircleRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
        });

        list.CountingCircles_.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminOnChildDoiShouldReturnOk()
    {
        var list = await AdminClient.ListAssignableAsync(new ListAssignableCountingCircleRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });

        list.CountingCircles_.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminOnDeepNestedChildDoiShouldReturnOk()
    {
        var list = await AdminClient.ListAssignableAsync(new ListAssignableCountingCircleRequest
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil,
        });

        list.CountingCircles_.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .ListAssignableAsync(new ListAssignableCountingCircleRequest
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            });

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }
}
