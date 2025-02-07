// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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

public class ContestListPoliticalBusinessSummaryTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public ContestListPoliticalBusinessSummaryTest(TestApplicationFactory factory)
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
    public async Task TestAsAdminShouldReturnAllPoliticalBusinesses()
    {
        var response = await AdminClient.ListPoliticalBusinessSummariesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnParentAndOwnPoliticalBusinesses()
    {
        var response = await ElectionAdminUzwilClient.ListPoliticalBusinessSummariesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.ListPoliticalBusinessSummariesAsync(new ListPoliticalBusinessSummariesRequest
            {
                ContestId = ContestMockedData.IdKirche,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestInvalidGuidShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ListPoliticalBusinessSummariesAsync(new ListPoliticalBusinessSummariesRequest
            {
                ContestId = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.ListPoliticalBusinessSummariesAsync(new ListPoliticalBusinessSummariesRequest
            {
                ContestId = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ListSummariesAsync(new ListContestSummariesRequest());
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

    private ListPoliticalBusinessSummariesRequest NewValidRequest(
        Action<ListPoliticalBusinessSummariesRequest>? customizer = null)
    {
        var request = new ListPoliticalBusinessSummariesRequest
        {
            ContestId = ContestMockedData.IdBundContest,
        };

        customizer?.Invoke(request);
        return request;
    }
}
