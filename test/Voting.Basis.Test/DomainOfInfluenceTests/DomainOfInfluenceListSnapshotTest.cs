// (c) Copyright 2024 by Abraxas Informatik AG
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
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceListSnapshotTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceListSnapshotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await HistoricalMockedData.SeedHistory(RunScoped);
    }

    [Fact]
    public async Task TestListHistorizationElectionAdmin()
    {
        var listEmpty = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent3, HistoricalMockedData.CcStGallenId);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        var listWithTwoDoi = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent5, HistoricalMockedData.CcStGallenId);
        listWithTwoDoi.MatchSnapshot("listWithTwoDoi");
        listWithTwoDoi.Should().HaveCount(2);

        // empty because cc "Frauenfeld" is only assigned to dois which are not directly accessible by current tenant.
        var listEmpty2 = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent6, HistoricalMockedData.CcFrauenfeldId);
        listEmpty2.MatchSnapshot("listEmpty2");
        listEmpty2.Should().HaveCount(0);

        var listWithTwoDoiAndOneUpdatedDoi = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent9, HistoricalMockedData.CcStGallenId);
        listWithTwoDoiAndOneUpdatedDoi.MatchSnapshot("listWithTwoDoiAndOneUpdatedDoi");
        listWithTwoDoiAndOneUpdatedDoi.Should().HaveCount(2);

        var listEmptyAfterUnassign = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent10, HistoricalMockedData.CcStGallenId);
        listEmptyAfterUnassign.MatchSnapshot("listEmptyAfterUnassign");
        listEmptyAfterUnassign.Should().HaveCount(0);

        var listWithTwoDoiWhereOneUpdatedCc2 = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent11, HistoricalMockedData.CcStGallenId);
        listWithTwoDoiWhereOneUpdatedCc2.MatchSnapshot("listWithTwoDoiWhereOneUpdatedCc2");
        listWithTwoDoiWhereOneUpdatedCc2.Should().HaveCount(2);

        var listEmptyAfterParentDoiDelete = await ElectionAdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14, HistoricalMockedData.CcStGallenId);
        listEmptyAfterParentDoiDelete.MatchSnapshot("listEmptyAfterParentDoiDelete");
        listEmptyAfterParentDoiDelete.Should().HaveCount(0);
    }

    [Fact]
    public async Task TestListHistorizationAdmin()
    {
        var listEmpty = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent3, HistoricalMockedData.CcStGallenId);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        var listWithTwoDoi = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent5, HistoricalMockedData.CcStGallenId);
        listWithTwoDoi.MatchSnapshot("listWithTwoDoi");
        listWithTwoDoi.Should().HaveCount(2);

        var listWithTwoDoi2 = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent6, HistoricalMockedData.CcFrauenfeldId);
        listWithTwoDoi2.MatchSnapshot("listWithTwoDoi2");
        listWithTwoDoi2.Should().HaveCount(2);

        var listWithTwoDoiAndOneUpdatedDoi = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent9, HistoricalMockedData.CcStGallenId);
        listWithTwoDoiAndOneUpdatedDoi.MatchSnapshot("listWithTwoDoiAndOneUpdatedDoi");
        listWithTwoDoiAndOneUpdatedDoi.Should().HaveCount(2);

        var listEmptyAfterUnassign = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent10, HistoricalMockedData.CcStGallenId);
        listEmptyAfterUnassign.MatchSnapshot("listEmptyAfterUnassign");
        listEmptyAfterUnassign.Should().HaveCount(0);

        var listWithTwoDoiWhereOneUpdatedCc2 = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent11, HistoricalMockedData.CcStGallenId);
        listWithTwoDoiWhereOneUpdatedCc2.MatchSnapshot("listWithTwoDoiWhereOneUpdatedCc2");
        listWithTwoDoiWhereOneUpdatedCc2.Should().HaveCount(2);

        var listEmptyAfterParentDoiDelete = await AdminListSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14, HistoricalMockedData.CcStGallenId);
        listEmptyAfterParentDoiDelete.MatchSnapshot("listEmptyAfterParentDoiDelete");
        listEmptyAfterParentDoiDelete.Should().HaveCount(0);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .ListSnapshotAsync(new ListDomainOfInfluenceSnapshotRequest
            {
                CountingCircleId = HistoricalMockedData.CcStGallenId,
                DateTime = Timestamp.FromDateTime(HistoricalMockedData.DateTimeAfterEvent3),
            });

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private async Task<IEnumerable<ProtoModels.DomainOfInfluence>> ElectionAdminListSnapshotRequest(DateTime dateTime, string countingCircleId)
    {
        var result = await ElectionAdminClient.ListSnapshotAsync(new ListDomainOfInfluenceSnapshotRequest
        {
            CountingCircleId = countingCircleId,
            DateTime = Timestamp.FromDateTime(dateTime),
        });
        return result.DomainOfInfluences_;
    }

    private async Task<IEnumerable<ProtoModels.DomainOfInfluence>> AdminListSnapshotRequest(DateTime dateTime, string countingCircleId)
    {
        var result = await AdminClient.ListSnapshotAsync(new ListDomainOfInfluenceSnapshotRequest
        {
            CountingCircleId = countingCircleId,
            DateTime = Timestamp.FromDateTime(dateTime),
        });
        return result.DomainOfInfluences_;
    }
}
