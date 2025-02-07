// (c) Copyright by Abraxas Informatik AG
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

namespace Voting.Basis.Test.VoteTests;

public class VoteGetTest : PoliticalBusinessAuthorizationGrpcBaseTest<VoteService.VoteServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";

    public VoteGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetAsync(new GetVoteRequest
        {
            Id = VoteMockedData.IdGossauVoteInContestStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsCantonAdminShouldReturnOk()
    {
        var response = await ZurichCantonAdminClient.GetAsync(new GetVoteRequest
        {
            Id = VoteMockedData.IdZurichVoteInContestZurich,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await CantonAdminClient.GetAsync(new GetVoteRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteService.VoteServiceClient(channel)
            .GetAsync(new GetVoteRequest
            {
                Id = VoteMockedData.IdGossauVoteInContestGossau,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
    }
}
