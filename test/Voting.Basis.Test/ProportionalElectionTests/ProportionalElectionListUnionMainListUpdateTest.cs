// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionMainListUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListUnionMainListUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAggregateSubListUnion()
    {
        var listUnionId = Guid.Parse(ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen);
        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionMainListUpdated
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen,
                ProportionalElectionMainListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
            });

        var listUnion = await CantonAdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen,
        });

        listUnion.MatchSnapshot();

        var lists = await RunOnDb(db =>
            db.ProportionalElectionListUnionEntries
                .Include(x => x.ProportionalElectionList)
                .OrderBy(x => x.ProportionalElectionList.Position)
                .Where(x => x.ProportionalElectionListUnionId == listUnionId).ToListAsync());
        lists.Should().HaveCount(2);
        lists[0].ProportionalElectionList.SubListUnionDescription
            .Should()
            .Be(
                "<span><span>1a</span>, <span class=\"main-list\">1a</span>, <span class=\"main-list\">2</span>, <span>2</span></span>");
        lists[1].ProportionalElectionList.SubListUnionDescription
            .Should()
            .Be(
                "<span><span>1a</span>, <span class=\"main-list\">1a</span>, <span class=\"main-list\">2</span>, <span>2</span>, <span>2</span>, <span class=\"main-list\">3a</span></span>");
    }

    [Fact]
    public async Task TestAggregateListUnion()
    {
        var listUnionId = Guid.Parse(ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen);
        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionMainListUpdated
            {
                ProportionalElectionListUnionId = listUnionId.ToString(),
                ProportionalElectionMainListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
            });

        var listUnion = await CantonAdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
        });

        listUnion.MatchSnapshot();

        var lists = await RunOnDb(db =>
            db.ProportionalElectionListUnionEntries
                .Include(x => x.ProportionalElectionList)
                .OrderBy(x => x.ProportionalElectionList.Position)
                .Where(x => x.ProportionalElectionListUnionId == listUnionId).ToListAsync());
        lists.Should().HaveCount(3);

        lists[0].ProportionalElectionList.ListUnionDescription
            .Should()
            .Be("<span><span>1a</span>, <span>1a</span>, <span class=\"main-list\">2</span>, <span>2</span>, <span>3a</span></span>");

        lists[1].ProportionalElectionList.ListUnionDescription
            .Should()
            .Be("<span><span>1a</span>, <span>1a</span>, <span class=\"main-list\">2</span>, <span>2</span>, <span>2</span>, <span>3a</span>, <span>3a</span></span>");

        lists[2].ProportionalElectionList.ListUnionDescription
            .Should()
            .Be("<span><span>1a</span>, <span class=\"main-list\">2</span>, <span>2</span>, <span>3a</span>, <span>3a</span></span>");
    }

    [Fact]
    public async Task ListUnionShouldReturnOk()
    {
        await CantonAdminClient.UpdateListUnionMainListAsync(NewValidRequest(l =>
            l.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen));
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionMainListUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task SubListUnionShouldReturnOk()
    {
        await CantonAdminClient.UpdateListUnionMainListAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionMainListUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task ListUnionNoMainListShouldReturnOk()
    {
        await CantonAdminClient.UpdateListUnionMainListAsync(NewValidRequest(l => l.ProportionalElectionMainListId = string.Empty));
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionMainListUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.UpdateListUnionMainListAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionMainListUpdated>();
        });
    }

    [Fact]
    public async Task ForeignMainListShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateListUnionMainListAsync(NewValidRequest(l =>
                l.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen)),
            StatusCode.InvalidArgument,
            "list");
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateListUnionMainListAsync(new UpdateProportionalElectionListUnionMainListRequest
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
                ProportionalElectionMainListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateListUnionMainListAsync(NewValidRequest(x =>
            {
                x.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionEVotingApprovedInContestStGallen;
                x.ProportionalElectionMainListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    [Fact]
    public async Task ForeignSubListUnionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateListUnionMainListAsync(NewValidRequest(l =>
                l.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestGossau)),
            StatusCode.InvalidArgument);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .UpdateListUnionMainListAsync(NewValidRequest());

    private UpdateProportionalElectionListUnionMainListRequest NewValidRequest(
        Action<UpdateProportionalElectionListUnionMainListRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionListUnionMainListRequest
        {
            ProportionalElectionListUnionId = ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen,
            ProportionalElectionMainListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
        };

        customizer?.Invoke(request);
        return request;
    }
}
