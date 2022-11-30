// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupsListTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";

    public MajorityElectionBallotGroupsListTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await ElectionAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminParentDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
            {
                MajorityElectionId = MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestAsElectionAdminParentDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
            {
                MajorityElectionId = MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestAsAdminChildDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
            {
                MajorityElectionId = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestAsElectionAdminChildDomainOfInfluenceShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
            {
                MajorityElectionId = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestForeignDoiShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
            {
                MajorityElectionId = MajorityElectionMockedData.IdKircheMajorityElectionInContestKircheWithoutChilds,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
            {
                MajorityElectionId = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .ListBallotGroupsAsync(new ListMajorityElectionBallotGroupsRequest
            {
                MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau,
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
