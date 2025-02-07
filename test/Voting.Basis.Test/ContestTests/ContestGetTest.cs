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

namespace Voting.Basis.Test.ContestTests;

public class ContestGetTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public ContestGetTest(TestApplicationFactory factory)
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

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await ElectionAdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminShouldReturnAllPoliticalBusiness()
    {
        var response = await AdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdStGallenEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminOwnContestShouldReturnAllPoliticalBusiness()
    {
        var response = await ElectionAdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdStGallenEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminParentContestShouldReturnAllPoliticalBusiness()
    {
        var response = await AdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdBundContest,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminParentContestShouldReturnOwnAndParentPoliticalBusiness()
    {
        var response = await ElectionAdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdBundContest,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.GetAsync(new GetContestRequest
            {
                Id = ContestMockedData.IdKirche,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestInvalidGuidShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.GetAsync(new GetContestRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.GetAsync(new GetContestRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .GetAsync(new GetContestRequest
            {
                Id = ContestMockedData.IdGossau,
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
