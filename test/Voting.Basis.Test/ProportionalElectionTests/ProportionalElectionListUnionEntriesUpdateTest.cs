// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionEntriesUpdateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListUnionEntriesUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var listUnionId = Guid.Parse(ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen);
        var entries = new ProportionalElectionListUnionEntriesEventData
        {
            ProportionalElectionListUnionId = listUnionId.ToString(),
        };

        entries.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen);
        entries.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen);
        entries.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionEntriesUpdated
            {
                ProportionalElectionListUnionEntries = entries,
            });

        var result = await AdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
        });
        result.MatchSnapshot();

        var lists = await RunOnDb(db =>
            db.ProportionalElectionListUnionEntries
                .Include(x => x.ProportionalElectionList)
                .OrderBy(x => x.ProportionalElectionList.Position)
                .Where(x => x.ProportionalElectionListUnionId == listUnionId).ToListAsync());
        lists.Should().HaveCount(3);
        lists[0].ProportionalElectionList.ListUnionDescription[Languages.German]
            .Should()
            .Be("<span><span>Liste 1 de</span>, <span>Liste 2 de</span>, <span>Liste 3 de</span>, <span>…</span></span>");
        lists[1].ProportionalElectionList.ListUnionDescription[Languages.German]
            .Should()
            .Be("<span><span>Liste 1 de</span>, <span>Liste 2 de</span>, <span>Liste 3 de</span>, <span>…</span></span>");
        lists[2].ProportionalElectionList.ListUnionDescription[Languages.German]
            .Should()
            .Be("<span><span>Liste 1 de</span>, <span>Liste 2 de</span>, <span>Liste 3 de</span>, <span>…</span></span>");
    }

    [Fact]
    public async Task TestAggregateCascadingDelete()
    {
        var entries = new ProportionalElectionListUnionEntriesEventData
        {
            ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
        };

        entries.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionEntriesUpdated
            {
                ProportionalElectionListUnionEntries = entries,
            });

        var result = await AdminClient.GetListUnionAsync(new GetProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
        });
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ListUnionShouldReturnOk()
    {
        await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionEntriesUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task SubListUnionShouldReturnOk()
    {
        await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest(lu =>
        {
            lu.ProportionalElectionListUnionId = ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen;
            lu.ProportionalElectionListIds.Clear();
            lu.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen);
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionEntriesUpdated>();

        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionEntriesUpdated>();
        });
    }

    [Fact]
    public async Task NoLists()
    {
        await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest(lu => lu.ProportionalElectionListIds.Clear()));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionEntriesUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task ListFromSameProportionalElectionButNotInRootListUnionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest(l =>
            {
                l.ProportionalElectionListUnionId = ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen;
                l.ProportionalElectionListIds.Clear();
                l.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen);
            })),
            StatusCode.InvalidArgument,
            "RootListUnion");
    }

    [Fact]
    public async Task ForeignListUnionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest(l =>
                l.ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestGossau)),
            StatusCode.InvalidArgument,
            "ListIds");
    }

    [Fact]
    public async Task ForeignListShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest(b =>
            {
                b.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListIdGossauProportionalElectionInContestGossau);
            })),
            StatusCode.InvalidArgument,
            "ListIds");
    }

    [Fact]
    public async Task DuplicateListThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionEntriesAsync(NewValidRequest(b =>
            {
                b.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen);
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateListUnionEntriesAsync(new UpdateProportionalElectionListUnionEntriesRequest
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
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
            async () => await ElectionAdminClient.UpdateListUnionEntriesAsync(new()
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
            }),
            StatusCode.InvalidArgument,
            "The election does not distribute mandates per Hagenbach-Bischoff algorithm");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .UpdateListUnionEntriesAsync(NewValidRequest());

    private UpdateProportionalElectionListUnionEntriesRequest NewValidRequest(
        Action<UpdateProportionalElectionListUnionEntriesRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionListUnionEntriesRequest
        {
            ProportionalElectionListUnionId = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
        };

        request.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen);
        request.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen);
        request.ProportionalElectionListIds.Add(ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen);

        customizer?.Invoke(request);
        return request;
    }
}
