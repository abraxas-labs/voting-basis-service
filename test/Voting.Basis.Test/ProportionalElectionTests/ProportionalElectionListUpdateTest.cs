// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUpdateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        var request = NewValidRequest();
        var response = await AdminClient.UpdateListAsync(request);

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUpdated, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionList.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElectionList.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateListAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var listId = Guid.Parse(ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen);
        await TestEventPublisher.Publish(
            new ProportionalElectionListUpdated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = listId.ToString(),
                    BlankRowCount = 2,
                    OrderNumber = "o2",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                    Description = { LanguageUtil.MockAllLanguages("Updated list") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated s.d. <script>alert(\"hi\");</script>") },
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                },
            });

        var list = await AdminClient.GetListAsync(new GetProportionalElectionListRequest
        {
            Id = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
        });
        list.MatchSnapshot();

        var lists = await RunOnDb(db => db.ProportionalElectionListUnionEntries
            .Where(l => l.ProportionalElectionListId == listId)
            .SelectMany(x => x.ProportionalElectionListUnion.ProportionalElectionListUnionEntries)
            .Select(x => x.ProportionalElectionList)
            .OrderBy(x => x.Position)
            .ToListAsync());
        lists.Should().HaveCount(5);
        lists.Select(x => x.ListUnionDescription[Languages.German])
            .Should()
            .BeEquivalentTo(
                "<span><span>Liste 1 de</span>, <span>Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; de</span>, <span>Liste 2 de</span></span>",
                "<span><span>Liste 1 de</span>, <span>Liste 1 de</span>, <span>Liste 2 de</span>, <span>…</span></span>",
                "<span><span>Liste 1 de</span>, <span>Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; de</span>, <span>Liste 2 de</span></span>",
                "<span><span>Liste 1 de</span>, <span>Liste 1 de</span>, <span>Liste 2 de</span>, <span>…</span></span>",
                "<span><span>Liste 1 de</span>, <span>Liste 1 de</span>, <span>Liste 2 de</span>, <span>…</span></span>");
        lists.Select(x => x.SubListUnionDescription[Languages.German])
            .Should()
            .BeEquivalentTo(
                "<span><span class=\"main-list\">Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; de</span>, <span>Liste 2 de</span></span>",
                "<span><span class=\"main-list\">Liste 1 de</span>, <span class=\"main-list\">Liste 1 de</span>, <span>Liste 2 de</span>, <span>…</span></span>",
                "<span><span class=\"main-list\">Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; de</span>, <span>Liste 2 de</span></span>",
                "<span><span class=\"main-list\">Liste 1 de</span>, <span class=\"main-list\">Liste 1 de</span>, <span>Liste 2 de</span>, <span>…</span></span>",
                "<span><span class=\"main-list\">Liste 1 de</span>, <span class=\"main-list\">Liste 1 de</span>, <span>Liste 2 de</span>, <span>…</span></span>");
        lists.Select(x => x.ListUnionDescription[Languages.French])
            .Should()
            .BeEquivalentTo(
                "<span><span>Liste 1 fr</span>, <span>Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; fr</span>, <span>Liste 2 fr</span></span>",
                "<span><span>Liste 1 fr</span>, <span>Liste 1 fr</span>, <span>Liste 2 fr</span>, <span>…</span></span>",
                "<span><span>Liste 1 fr</span>, <span>Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; fr</span>, <span>Liste 2 fr</span></span>",
                "<span><span>Liste 1 fr</span>, <span>Liste 1 fr</span>, <span>Liste 2 fr</span>, <span>…</span></span>",
                "<span><span>Liste 1 fr</span>, <span>Liste 1 fr</span>, <span>Liste 2 fr</span>, <span>…</span></span>");
        lists.Select(x => x.SubListUnionDescription[Languages.French])
            .Should()
            .BeEquivalentTo(
                "<span><span class=\"main-list\">Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; fr</span>, <span>Liste 2 fr</span></span>",
                "<span><span class=\"main-list\">Liste 1 fr</span>, <span class=\"main-list\">Liste 1 fr</span>, <span>Liste 2 fr</span>, <span>…</span></span>",
                "<span><span class=\"main-list\">Updated s.d. &lt;script&gt;alert(&quot;hi&quot;);&lt;/script&gt; fr</span>, <span>Liste 2 fr</span></span>",
                "<span><span class=\"main-list\">Liste 1 fr</span>, <span class=\"main-list\">Liste 1 fr</span>, <span>Liste 2 fr</span>, <span>…</span></span>",
                "<span><span class=\"main-list\">Liste 1 fr</span>, <span class=\"main-list\">Liste 1 fr</span>, <span>Liste 2 fr</span>, <span>…</span></span>");

        await AssertHasPublishedMessage<ProportionalElectionListChangeMessage>(
            x => x.List.HasEqualIdAndNewEntityState(listId, EntityState.Modified));
    }

    [Fact]
    public async Task TestAggregateUnionLists()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListUpdated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                    BlankRowCount = 2,
                    OrderNumber = "3",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                    Description = { LanguageUtil.MockAllLanguages("Liste 3") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Liste 3") },
                },
            });

        (await RunOnDb(db =>
            db.ProportionalElectionUnionLists
                .Where(l => ProportionalElectionUnionMockedData.StGallen1.Id == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.ShortDescription })
                .OrderBy(x => x.OrderNumber)
                .ToListAsync())).MatchSnapshot();
    }

    [Fact]
    public async Task ForeignProportionalElectionListShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListAsync(NewValidRequest(l =>
                l.Id = ProportionalElectionMockedData.ListIdBundProportionalElectionInContestBund)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ChangeProportionalElectionIdShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListAsync(NewValidRequest(l =>
                l.ProportionalElectionId = ProportionalElectionMockedData.IdKircheProportionalElectionInContestKircheWithoutChilds)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MoreBlankRowsThanNumberOfMandatesShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListAsync(NewValidRequest(o => o.BlankRowCount = 564)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ChangePositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListAsync(NewValidRequest(o => o.Position = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionListUpdateAfterTestingPhaseShouldWork()
    {
        var listId = Guid.Parse(ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund);
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateListAsync(new UpdateProportionalElectionListRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
            Id = listId.ToString(),
            BlankRowCount = 0,
            Position = 2,
            OrderNumber = "2",
            Description = { LanguageUtil.MockAllLanguages("updated desc.") },
            ShortDescription = { LanguageUtil.MockAllLanguages("upd") },
        });

        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await AdminClient.GetListAsync(new GetProportionalElectionListRequest
        {
            Id = listId.ToString(),
        });
        election.MatchSnapshot("reponse");

        await AssertHasPublishedMessage<ProportionalElectionListChangeMessage>(
            x => x.List.HasEqualIdAndNewEntityState(listId, EntityState.Modified));
    }

    [Fact]
    public async Task ProportionalElectionListUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateListAsync(NewValidRequest(o =>
            {
                o.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
                o.Id = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund;
                o.Position = 2;
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task ProportionalElectionListInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.UpdateListAsync(NewValidRequest(o =>
            {
                o.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
                o.Id = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund;
                o.Position = 2;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .UpdateListAsync(NewValidRequest());

    private UpdateProportionalElectionListRequest NewValidRequest(
        Action<UpdateProportionalElectionListRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionListRequest
        {
            Id = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
            BlankRowCount = 1,
            OrderNumber = "upd1",
            Position = 1,
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            Description = { LanguageUtil.MockAllLanguages("Updated list") },
            ShortDescription = { LanguageUtil.MockAllLanguages("updated s.d.") },
            PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
        };

        customizer?.Invoke(request);
        return request;
    }
}
