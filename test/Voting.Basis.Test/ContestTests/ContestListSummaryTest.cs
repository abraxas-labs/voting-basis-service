// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ContestTests;

public class ContestListSummaryTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestListSummaryTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await VoteMockedData.Seed(RunScoped, false);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAllContests()
    {
        var response = await AdminClient.ListSummariesAsync(new ListContestSummariesRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnParentAndOwnContests()
    {
        var response = await ElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldFilterByState()
    {
        var response = await AdminClient.ListSummariesAsync(new ListContestSummariesRequest
        {
            States =
                {
                    SharedProto.ContestState.Archived,
                    SharedProto.ContestState.TestingPhase,
                },
        });
        response.ContestSummaries_.Count(x => x.State == SharedProto.ContestState.Archived).Should().Be(1);
        response.ContestSummaries_.Count(x => x.State == SharedProto.ContestState.TestingPhase).Should().Be(6);
        response.ContestSummaries_.Should().HaveCount(7);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ListSummariesAsync(new ListContestSummariesRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
