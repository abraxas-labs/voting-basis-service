// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionMainListUpdateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
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

        var listUnion = await AdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
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
        lists[0].ProportionalElectionList.SubListUnionDescription[Languages.German]
            .Should()
            .Be(
                "<span><span>Liste 1 de</span>, <span class=\"main-list\">Liste 1 de</span>, <span class=\"main-list\">Liste 2 de</span>, <span>…</span></span>");
        lists[1].ProportionalElectionList.SubListUnionDescription[Languages.German]
            .Should()
            .Be(
                "<span><span>Liste 1 de</span>, <span class=\"main-list\">Liste 1 de</span>, <span class=\"main-list\">Liste 2 de</span>, <span>…</span></span>");
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

        var listUnion = await AdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
        });

        listUnion.MatchSnapshot();

        var lists = await RunOnDb(db =>
            db.ProportionalElectionListUnionEntries
                .Include(x => x.ProportionalElectionList)
                .Where(x => x.ProportionalElectionListUnionId == listUnionId).ToListAsync());
        lists.Should().HaveCount(3);

        var singleListUnionId = Guid.Parse(ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen);
        foreach (var list in lists)
        {
            if (list.ProportionalElectionListId == singleListUnionId)
            {
                list.ProportionalElectionList.ListUnionDescription[Languages.German]
                    .Should()
                    .Be(
                        "<span><span>Liste 1 de</span>, <span class=\"main-list\">Liste 2 de</span>, <span>Liste 3 de</span></span>");
                continue;
            }

            list.ProportionalElectionList.ListUnionDescription[Languages.German]
                .Should()
                .Be(
                    "<span><span>Liste 1 de</span>, <span class=\"main-list\">Liste 2 de</span>, <span>Liste 3 de</span>, <span>…</span></span>");
        }
    }

    [Fact]
    public async Task ListUnionShouldReturnOk()
    {
        await AdminClient.UpdateListUnionMainListAsync(NewValidRequest(l =>
            l.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen));
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionMainListUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task SubListUnionShouldReturnOk()
    {
        await AdminClient.UpdateListUnionMainListAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionMainListUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateListUnionMainListAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionMainListUpdated>();
        });
    }

    [Fact]
    public async Task ForeignMainListShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionMainListAsync(NewValidRequest(l =>
                l.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen)),
            StatusCode.InvalidArgument,
            "list");
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionMainListAsync(new UpdateProportionalElectionListUnionMainListRequest
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
                ProportionalElectionMainListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ListUnionInNonHagenbachBischoffElectionShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.Id == DomainOfInfluenceMockedData.GuidGossau,
            doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms = new()
            {
                ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            });

        await ElectionAdminClient.UpdateAsync(new()
        {
            Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
            PoliticalBusinessNumber = "1",
            NumberOfMandates = 2,
            MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            ContestId = ContestMockedData.IdBundContest,
            ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
        });

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateListUnionMainListAsync(new()
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
            }),
            StatusCode.InvalidArgument,
            "The election does not distribute mandates per Hagenbach-Bischoff algorithm");
    }

    [Fact]
    public async Task ForeignSubListUnionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionMainListAsync(NewValidRequest(l =>
                l.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestGossau)),
            StatusCode.InvalidArgument);
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
