// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleListSnapshotTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    public CountingCircleListSnapshotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await HistoricalMockedData.SeedHistory(RunScoped);
    }

    [Fact]
    public async Task TestListHistorizationElectionAdminWithDeleted()
    {
        var listEmpty = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeBeforeEvents);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        // does not include "Thurgau" or "Gossau", because foreign tenant and the current tenant does not own a parent doi.
        var listWithOneCc = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent1);
        listWithOneCc.MatchSnapshot("listWithOneCc");
        listWithOneCc.Should().HaveCount(1);

        // includes Cc "Gossau" from foreign Tenant, because the Doi "St.Gallen" belongs to the current tenant.
        var listWithTwoWhereOneUpdatedCc = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent11);
        listWithTwoWhereOneUpdatedCc.MatchSnapshot("listWithTwoWhereOneUpdatedCc");
        listWithTwoWhereOneUpdatedCc.Should().HaveCount(2);

        var listWithTwoCcWhereOneDeletedCc = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent12);
        listWithTwoCcWhereOneDeletedCc.MatchSnapshot("listWithTwoCcWhereOneDeletedCc");
        listWithTwoCcWhereOneDeletedCc.Should().HaveCount(2);
        listWithTwoCcWhereOneDeletedCc.Where(x => x.Info.DeletedOn != null).Should().HaveCount(1);

        // Cc "Gossau" not visible anymore, because the Doi "St.Gallen" got deleted.
        var listWithOneCcAndNoDeletedCc = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent13);
        listWithOneCcAndNoDeletedCc.MatchSnapshot("listWithOneCcAndNoDeletedCc");
        listWithOneCcAndNoDeletedCc.Should().HaveCount(1);
        listWithOneCcAndNoDeletedCc.Where(x => x.Info.DeletedOn != null).Should().HaveCount(0);
    }

    [Fact]
    public async Task TestListHistorizationAdminWithDeleted()
    {
        var listEmpty = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeBeforeEvents);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        // also includes Cc "Thurgau" from foreign tenant
        var listWithTwoCc = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent1);
        listWithTwoCc.MatchSnapshot("listWithTwoCc");
        listWithTwoCc.Should().HaveCount(2);

        var listWithThreeCcWhereOneUpdatedCc = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent8);
        listWithThreeCcWhereOneUpdatedCc.MatchSnapshot("listWithThreeCcWhereOneUpdatedCc");
        listWithThreeCcWhereOneUpdatedCc.Should().HaveCount(3);

        var listWithThreeCcAndOneDeletedCc = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent12);
        listWithThreeCcAndOneDeletedCc.MatchSnapshot("listWithThreeCcAndOneDeletedCc");
        listWithThreeCcAndOneDeletedCc.Should().HaveCount(3);
        listWithThreeCcAndOneDeletedCc.Where(x => x.Info.DeletedOn != null).Should().HaveCount(1);
    }

    [Fact]
    public async Task TestListHistorizationElectionAdminWithNoDeleted()
    {
        var listWithOneCcAndNoDeletedCc = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent12, false);
        listWithOneCcAndNoDeletedCc.MatchSnapshot("listWithOneCcAndNoDeletedCc");
        listWithOneCcAndNoDeletedCc.Should().HaveCount(1);
    }

    [Fact]
    public async Task TestListMergedHistorizationAdmin()
    {
        var listWithScheduledRapperswilJonaMerge = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent17);
        GetRapperswilJonaList(listWithScheduledRapperswilJonaMerge).MatchSnapshot("listWithScheduledRapperswilJonaMerge");

        var listWithActivatedRapperswilJonaMerge = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent20);
        GetRapperswilJonaList(listWithActivatedRapperswilJonaMerge).MatchSnapshot("listWithActivatedRapperswilJonaMerge");
    }

    [Fact]
    public async Task TestListHistorizationAdminWithNoDeleted()
    {
        var listWithOneCcAndNoDeletedCc = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent12, false);
        listWithOneCcAndNoDeletedCc.MatchSnapshot("listWithOneCcAndNoDeletedCc");
        listWithOneCcAndNoDeletedCc.Should().HaveCount(2);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .ListSnapshotAsync(new ListCountingCircleSnapshotRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private async Task<IEnumerable<ProtoModels.CountingCircle>> ElectionAdminListSnapshotRequest(
        DateTime dateTime,
        bool includeDeleted = true)
    {
        var result = await ElectionAdminClient.ListSnapshotAsync(new ListCountingCircleSnapshotRequest
        {
            DateTime = Timestamp.FromDateTime(dateTime),
            IncludeDeleted = includeDeleted,
        });
        return result.CountingCircles_;
    }

    private async Task<IEnumerable<ProtoModels.CountingCircle>> AdminListSnapshotRequest(
        DateTime dateTime,
        bool includeDeleted = true)
    {
        var result = await AdminClient.ListSnapshotAsync(new ListCountingCircleSnapshotRequest
        {
            DateTime = Timestamp.FromDateTime(dateTime),
            IncludeDeleted = includeDeleted,
        });
        return result.CountingCircles_;
    }

    private List<ProtoModels.CountingCircle> GetRapperswilJonaList(IEnumerable<ProtoModels.CountingCircle> ccs)
    {
        return ccs
            .Where(cc => cc.Id == HistoricalMockedData.CcIdRapperswilJona
                            || cc.Id == HistoricalMockedData.CcIdJona
                            || cc.Id == HistoricalMockedData.CcIdRapperswil)
            .ToList();
    }
}
