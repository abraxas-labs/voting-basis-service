// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestListFuturePreconfiguredDatesTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestListFuturePreconfiguredDatesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await PreconfiguredContestDateMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AdminClient.ListFuturePreconfiguredDatesAsync(new ListFuturePreconfiguredDatesRequest());
        response.PreconfiguredContestDates_.Should().HaveCount(5);
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await AdminClient.ListFuturePreconfiguredDatesAsync(new ListFuturePreconfiguredDatesRequest());
        response.PreconfiguredContestDates_.Should().HaveCount(5);
        response.PreconfiguredContestDates_[0].Date.ToDateTime().Should().Be(new DateTime(2020, 2, 9, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task TestShouldContainOnlyFutureDates()
    {
        var today = MockedClock.UtcNowDate.Date;
        var date1 = today.AddDays(10);
        var date2 = today.AddYears(1);
        await RunOnDb(db =>
        {
            db.RemoveRange(db.PreconfiguredContestDates);
            db.PreconfiguredContestDates.AddRange(
                new PreconfiguredContestDate { Id = today.AddDays(-1) },
                new PreconfiguredContestDate { Id = today.AddDays(-20) },
                new PreconfiguredContestDate { Id = date1 },
                new PreconfiguredContestDate { Id = date2 });
            return db.SaveChangesAsync();
        });

        var response = await AdminClient.ListFuturePreconfiguredDatesAsync(new ListFuturePreconfiguredDatesRequest());
        response.PreconfiguredContestDates_.Should().HaveCount(2);
        response.PreconfiguredContestDates_
            .Select(x => x.Date.ToDateTime())
            .Should()
            .BeEquivalentTo(new[] { date1, date2 });
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
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
