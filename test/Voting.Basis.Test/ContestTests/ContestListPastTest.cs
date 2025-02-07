// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestListPastTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestListPastTest(TestApplicationFactory factory)
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
    public async Task TestAsElectionAdminShouldReturn()
    {
        var result = await ElectionAdminClient.ListPastAsync(NewValidRequest());
        result.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsAdminShouldReturn()
    {
        var result = await AdminClient.ListPastAsync(NewValidRequest());
        result.MatchSnapshot();
    }

    [Fact]
    public async Task TestSameDayAsOtherContestDate()
    {
        var result = await AdminClient.ListPastAsync(NewValidRequest(x => x.Date = new DateTime(2018, 10, 2, 1, 0, 0, DateTimeKind.Utc).ToTimestamp()));
        result.Contests_.Should().HaveCount(0);
    }

    [Fact]
    public async Task NotOwnerOfDomainOfInfluenceShouldReturnEmpty()
    {
        var client = new ContestService.ContestServiceClient(
            CreateGrpcChannel(
                tenant: DomainOfInfluenceMockedData.Uzwil.SecureConnectId,
                roles: Roles.Admin));

        var result = await client.ListPastAsync(NewValidRequest());
        result.Contests_.Should().HaveCount(0);
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

    private ListContestPastRequest NewValidRequest(
        Action<ListContestPastRequest>? customizer = null)
    {
        var request = new ListContestPastRequest
        {
            Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
        };
        customizer?.Invoke(request);
        return request;
    }
}
