// (c) Copyright by Abraxas Informatik AG
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

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceListTreeSnapshotTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceListTreeSnapshotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await HistoricalMockedData.SeedHistory(RunScoped);
    }

    [Fact]
    public async Task TestListTreeHistorizationElectionAdminWithDeleted()
    {
        var listEmpty = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeBeforeEvents);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        // Doi "Bund" exists but the current tenant does not own it.
        var listEmpty2 = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent2);
        listEmpty2.MatchSnapshot("listEmpty2");
        listEmpty2.Should().HaveCount(0);

        var listWithOneRootAndOneChild = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent3);
        listWithOneRootAndOneChild.MatchSnapshot("listWithOneRootAndOneChild");
        listWithOneRootAndOneChild.Should().HaveCount(1);
        listWithOneRootAndOneChild.First().Children.Should().HaveCount(1);

        // Doi "Bund" is included because it is a parent, but doi "Thurgau" is not included, since it is a sibling of a doi which belongs to the current tenant.
        var listWithOneRootAndOneChild2 = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent4);
        listWithOneRootAndOneChild2.MatchSnapshot("listWithOneRootAndOneChild2");
        listWithOneRootAndOneChild2.Should().HaveCount(1);
        listWithOneRootAndOneChild2.First().Children.Should().HaveCount(1);

        var listWithOneRootAndOneChildWhereOneDeleted = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent13);
        listWithOneRootAndOneChildWhereOneDeleted.MatchSnapshot("listWithOneRootAndOneChildWhereOneDeleted");
        listWithOneRootAndOneChildWhereOneDeleted.Should().HaveCount(1);
        listWithOneRootAndOneChildWhereOneDeleted.First().Children.Should().HaveCount(1);
        listWithOneRootAndOneChildWhereOneDeleted.First().Children.Where(x => x.Info.DeletedOn != null).Should().HaveCount(1);

        var listWithOneRootAndOneChildAllDeleted = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14);
        listWithOneRootAndOneChildAllDeleted.MatchSnapshot("listWithOneRootAndOneChildAllDeleted");
        listWithOneRootAndOneChildAllDeleted.Should().HaveCount(1);
        listWithOneRootAndOneChildAllDeleted.Where(x => x.Info.DeletedOn != null).Should().HaveCount(1);
        listWithOneRootAndOneChildAllDeleted.First().Children.Should().HaveCount(1);
        listWithOneRootAndOneChildAllDeleted.First().Children.Where(x => x.Info.DeletedOn != null).Should().HaveCount(1);
    }

    [Fact]
    public async Task TestListTreeHistorizationAdminWithDeleted()
    {
        var listEmpty = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeBeforeEvents);
        listEmpty.MatchSnapshot("listEmpty");
        listEmpty.Should().HaveCount(0);

        var listWithOneRoot = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent2);
        listWithOneRoot.MatchSnapshot("listWithOneRoot");
        listWithOneRoot.Should().HaveCount(1);
        listWithOneRoot.First().Children.Should().HaveCount(0);

        var listWithOneRootAndOneChild = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent3);
        listWithOneRootAndOneChild.MatchSnapshot("listWithOneRootAndOneChild");
        listWithOneRootAndOneChild.Should().HaveCount(1);
        listWithOneRootAndOneChild.First().Children.Should().HaveCount(1);

        var listWithOneRootAndTwoChild = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent4);
        listWithOneRootAndTwoChild.MatchSnapshot("listWithOneRootAndTwoChild");
        listWithOneRootAndTwoChild.Should().HaveCount(1);
        listWithOneRootAndTwoChild.First().Children.Should().HaveCount(2);

        var listWithOneRootAndTwoChildWhereOneDeleted = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent13);
        listWithOneRootAndTwoChildWhereOneDeleted.MatchSnapshot("listWithOneRootAndTwoChildWhereOneDeleted");
        listWithOneRootAndTwoChildWhereOneDeleted.Should().HaveCount(1);
        listWithOneRootAndTwoChildWhereOneDeleted.First().Children.Should().HaveCount(2);
        listWithOneRootAndTwoChildWhereOneDeleted.First().Children.Where(x => x.Info.DeletedOn != null).Should().HaveCount(1);

        var listWithOneRootAndTwoChildAllDeleted = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14);
        listWithOneRootAndTwoChildAllDeleted.MatchSnapshot("listWithOneRootAndTwoChildAllDeleted");
        listWithOneRootAndTwoChildAllDeleted.Should().HaveCount(1);
        listWithOneRootAndTwoChildAllDeleted.Where(x => x.Info.DeletedOn != null).Should().HaveCount(1);
        listWithOneRootAndTwoChildAllDeleted.First().Children.Should().HaveCount(2);
        listWithOneRootAndTwoChildAllDeleted.First().Children.Where(x => x.Info.DeletedOn != null).Should().HaveCount(2);
    }

    [Fact]
    public async Task TestListTreeHistorizationElectionAdminWithNoDeleted()
    {
        var listWithOneRootAndNoChild = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent12, false);

        // gossau deleted, sg and bund existing
        listWithOneRootAndNoChild.Select(x => x.Id).Should().BeEquivalentTo(HistoricalMockedData.DoiBundId);
        listWithOneRootAndNoChild.First().Children.Select(x => x.Id).Should().BeEquivalentTo(HistoricalMockedData.DoiStGallenId);

        var listEmptyAfterRootDelete = await ElectionAdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14, false);
        listEmptyAfterRootDelete.Should().HaveCount(0);
    }

    [Fact]
    public async Task TestListTreeHistorizationAdminWithNoDeleted()
    {
        var listWithOneRootAndOneChild = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent13, false);
        listWithOneRootAndOneChild.MatchSnapshot("listWithOneRootAndOneChild");
        listWithOneRootAndOneChild.Should().HaveCount(1);
        listWithOneRootAndOneChild.First().Children.Should().HaveCount(1);

        var listEmptyAfterRootDelete = await AdminListTreeSnapshotRequest(HistoricalMockedData.DateTimeAfterEvent14, false);
        listEmptyAfterRootDelete.MatchSnapshot("listEmptyAfterRootDelete");
        listEmptyAfterRootDelete.Should().HaveCount(0);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .ListTreeSnapshotAsync(new ListTreeDomainOfInfluenceSnapshotRequest
            {
                DateTime = Timestamp.FromDateTime(HistoricalMockedData.DateTimeAfterEvent12),
                IncludeDeleted = false,
            });

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
        yield return Roles.ApiReader;
    }

    private async Task<IEnumerable<ProtoModels.DomainOfInfluence>> ElectionAdminListTreeSnapshotRequest(DateTime dateTime, bool includeDeleted = true)
    {
        var result = await ElectionAdminClient.ListTreeSnapshotAsync(new ListTreeDomainOfInfluenceSnapshotRequest
        {
            DateTime = Timestamp.FromDateTime(dateTime),
            IncludeDeleted = includeDeleted,
        });
        return result.DomainOfInfluences_;
    }

    private async Task<IEnumerable<ProtoModels.DomainOfInfluence>> AdminListTreeSnapshotRequest(DateTime dateTime, bool includeDeleted = true)
    {
        var result = await AdminClient.ListTreeSnapshotAsync(new ListTreeDomainOfInfluenceSnapshotRequest
        {
            DateTime = Timestamp.FromDateTime(dateTime),
            IncludeDeleted = includeDeleted,
        });
        return result.DomainOfInfluences_;
    }
}
