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

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleListAssignedSnapshotTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    public CountingCircleListAssignedSnapshotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await HistoricalMockedData.SeedHistory(RunScoped);
    }

    [Fact]
    public async Task TestListForDomainOfInfluenceHistorizationElectionAdmin()
    {
        var listEmpty = await ElectionAdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent4, HistoricalMockedData.DoiBundId);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        // Ccs "Frauenfeld" and "Gossau" not visible, because the current tenant has no permissions for these ccs and does not own a parent of bund
        var listWithOneCc = await ElectionAdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent6, HistoricalMockedData.DoiBundId);
        listWithOneCc.MatchSnapshot("listWithOneCc");
        listWithOneCc.Should().HaveCount(1);

        var listEmptyAfterUnassign = await ElectionAdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent10, HistoricalMockedData.DoiBundId);
        listEmptyAfterUnassign.MatchSnapshot("listEmptyAfterUnassign");
        listEmptyAfterUnassign.Should().HaveCount(0);

        // The current tenant does not own "Bund" but a child of it ("St. Gallen"). A owned doi sees all parent dois.
        var listWithOneCc2 = await ElectionAdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent11, HistoricalMockedData.DoiBundId);
        listWithOneCc2.MatchSnapshot("listWithOneCc2");
        listWithOneCc2.Should().HaveCount(1);

        // no more doi's since everything is deleted
        var listWithNoCcs = await ElectionAdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14, HistoricalMockedData.DoiBundId);
        listWithNoCcs.Should().BeEmpty();

        // after cc merger activated the merged cc should be assigned
        var inheritedWithMergedCc = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent20, HistoricalMockedData.DoiRapperswilJonaId);
        inheritedWithMergedCc.Should().HaveCount(1);
    }

    [Fact]
    public async Task TestListForDomainOfInfluenceHistorizationAdmin()
    {
        var listEmpty = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent4, HistoricalMockedData.DoiBundId);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        var listWithTwoCc = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent6, HistoricalMockedData.DoiBundId);
        listWithTwoCc.MatchSnapshot("listWithTwoCc");
        listWithTwoCc.Should().HaveCount(2);

        var listWithOneAfterUnassign = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent10, HistoricalMockedData.DoiBundId);
        listWithOneAfterUnassign.MatchSnapshot("listWithOneAfterUnassign");
        listWithOneAfterUnassign.Should().HaveCount(1);

        var listWithThreeCc = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent11, HistoricalMockedData.DoiBundId);
        listWithThreeCc.MatchSnapshot("listWithThreeCc");
        listWithThreeCc.Should().HaveCount(3);

        var listWithOneCcAndNoDeletedCc = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent13, HistoricalMockedData.DoiBundId);
        listWithOneCcAndNoDeletedCc.MatchSnapshot("listWithOneCcAndNoDeletedCc");
        listWithOneCcAndNoDeletedCc.Should().HaveCount(1);

        // after delete parent doi the ccs should be cleared
        var emptyAfterParentDoiDeleted = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14, HistoricalMockedData.DoiBundId);
        emptyAfterParentDoiDeleted.Should().HaveCount(0);

        // after cc merger activated the merged cc should be assigned
        var inheritedWithMergedCc = await AdminListForDoiSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent20, HistoricalMockedData.DoiRapperswilJonaId);
        inheritedWithMergedCc.Should().HaveCount(1);
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

    private async Task<IEnumerable<ProtoModels.DomainOfInfluenceCountingCircle>> ElectionAdminListForDoiSnapshotRequest(
        DateTime dateTime,
        string domainOfInfluenceId)
    {
        var result = await ElectionAdminClient.ListAssignedSnapshotAsync(new ListAssignedCountingCircleSnapshotRequest
        {
            DomainOfInfluenceId = domainOfInfluenceId,
            DateTime = Timestamp.FromDateTime(dateTime),
        });
        return result.CountingCircles;
    }

    private async Task<IEnumerable<ProtoModels.DomainOfInfluenceCountingCircle>> AdminListForDoiSnapshotRequest(
        DateTime dateTime,
        string domainOfInfluenceId)
    {
        var result = await AdminClient.ListAssignedSnapshotAsync(new ListAssignedCountingCircleSnapshotRequest
        {
            DomainOfInfluenceId = domainOfInfluenceId,
            DateTime = Timestamp.FromDateTime(dateTime),
        });
        return result.CountingCircles;
    }
}
