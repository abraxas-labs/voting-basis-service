// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.ElectionGroupTests;

public class ElectionGroupListTest : BaseGrpcTest<ElectionGroupService.ElectionGroupServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";

    public ElectionGroupListTest(TestApplicationFactory factory)
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
        var response = await AdminClient.ListAsync(new ListElectionGroupsRequest
        {
            ContestId = MajorityElectionMockedData.StGallenMajorityElectionInContestBund.ContestId.ToString(),
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await AdminClient.ListAsync(new ListElectionGroupsRequest
        {
            ContestId = MajorityElectionMockedData.StGallenMajorityElectionInContestBund.ContestId.ToString(),
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminChildDomainOfInfluenceShouldReturnOk()
    {
        var response = await AdminClient.ListAsync(new ListElectionGroupsRequest
        {
            ContestId = MajorityElectionMockedData.UzwilMajorityElectionInContestStGallen.ContestId.ToString(),
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestForeignDoiAsAdminShouldReturnOk()
    {
        var response = await AdminClient.ListAsync(new ListElectionGroupsRequest
        {
            ContestId = MajorityElectionMockedData.KircheMajorityElectionInContestKirche.ContestId.ToString(),
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestForeignDoiAsElectionAdminShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ListAsync(new ListElectionGroupsRequest
            {
                ContestId = MajorityElectionMockedData.KircheMajorityElectionInContestKirche.ContestId.ToString(),
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.ListAsync(new ListElectionGroupsRequest
            {
                ContestId = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ElectionGroupService.ElectionGroupServiceClient(channel)
            .ListAsync(new ListElectionGroupsRequest
            {
                ContestId = MajorityElectionMockedData.StGallenMajorityElectionInContestBund.ContestId.ToString(),
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
