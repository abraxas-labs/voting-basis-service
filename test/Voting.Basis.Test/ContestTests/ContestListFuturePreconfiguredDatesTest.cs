// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestListFuturePreconfiguredDatesTest : BaseGrpcTest<ContestService.ContestServiceClient>, IDisposable
{
    public ContestListFuturePreconfiguredDatesTest(TestApplicationFactory factory)
        : base(factory)
    {
        // Need to use a date in the future to get the correct preconfigured contest dates
        AdjustableMockedClock.OverrideUtcNow = new DateTime(2030, 2, 3, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AdminClient.ListFuturePreconfiguredDatesAsync(new ListFuturePreconfiguredDatesRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await AdminClient.ListFuturePreconfiguredDatesAsync(new ListFuturePreconfiguredDatesRequest());
        response.PreconfiguredContestDates_[0].Date.ToDateTime().Should().Be(new DateTime(2030, 3, 24, 0, 0, 0, DateTimeKind.Utc));
    }

    public void Dispose()
    {
        AdjustableMockedClock.OverrideUtcNow = null;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ListFuturePreconfiguredDatesAsync(new ListFuturePreconfiguredDatesRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
        yield return Roles.EVotingAdmin;
    }
}
