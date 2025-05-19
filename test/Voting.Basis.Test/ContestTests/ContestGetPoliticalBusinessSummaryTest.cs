// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestGetPoliticalBusinessSummaryTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public ContestGetPoliticalBusinessSummaryTest(TestApplicationFactory factory)
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

        await MajorityElectionUnionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
    }

    [Theory]
    [InlineData(PoliticalBusinessType.MajorityElection, MajorityElectionMockedData.IdBundMajorityElectionInContestBund)]
    [InlineData(PoliticalBusinessType.SecondaryMajorityElection, MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund)]
    [InlineData(PoliticalBusinessType.ProportionalElection, ProportionalElectionMockedData.IdBundProportionalElectionInContestBund)]
    [InlineData(PoliticalBusinessType.Vote, VoteMockedData.IdBundVoteInContestBund)]
    public async Task TestAsAdminShouldWork(PoliticalBusinessType type, string id)
    {
        var response = await AdminClient.GetPoliticalBusinessSummaryAsync(new GetPoliticalBusinessSummaryRequest
        {
            PoliticalBusinessType = type,
            PoliticalBusinessId = id,
        });
        response.MatchSnapshot(type.ToString());
    }

    [Theory]
    [InlineData(PoliticalBusinessType.MajorityElection, MajorityElectionMockedData.IdBundMajorityElectionInContestBund)]
    [InlineData(PoliticalBusinessType.SecondaryMajorityElection, MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)]
    [InlineData(PoliticalBusinessType.ProportionalElection, ProportionalElectionMockedData.IdBundProportionalElectionInContestBund)]
    [InlineData(PoliticalBusinessType.Vote, VoteMockedData.IdBundVoteInContestBund)]
    public async Task TestAsElectionAdminShouldWork(PoliticalBusinessType type, string id)
    {
        var response = await ElectionAdminClient.GetPoliticalBusinessSummaryAsync(new GetPoliticalBusinessSummaryRequest
        {
            PoliticalBusinessType = type,
            PoliticalBusinessId = id,
        });
        response.MatchSnapshot(type.ToString());
    }

    [Fact]
    public async Task TestOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetPoliticalBusinessSummaryAsync(new GetPoliticalBusinessSummaryRequest
            {
                PoliticalBusinessType = PoliticalBusinessType.MajorityElection,
                PoliticalBusinessId = MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestInvalidGuidShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetPoliticalBusinessSummaryAsync(new GetPoliticalBusinessSummaryRequest
            {
                PoliticalBusinessType = PoliticalBusinessType.MajorityElection,
                PoliticalBusinessId = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetPoliticalBusinessSummaryAsync(new GetPoliticalBusinessSummaryRequest
            {
                PoliticalBusinessType = PoliticalBusinessType.MajorityElection,
                PoliticalBusinessId = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel).GetPoliticalBusinessSummaryAsync(new GetPoliticalBusinessSummaryRequest
        {
            PoliticalBusinessType = PoliticalBusinessType.Vote,
            PoliticalBusinessId = VoteMockedData.IdBundVoteInContestBund,
        });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
    }
}
