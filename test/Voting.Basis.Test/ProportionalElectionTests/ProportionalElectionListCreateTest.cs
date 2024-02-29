﻿// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListCreateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListCreateTest(TestApplicationFactory factory)
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
        var response = await AdminClient.CreateListAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListCreated, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionList.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElectionList.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateListAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListCreated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = "430f11f8-82bf-4f39-a2b9-d76e8c9dab08",
                    BlankRowCount = 0,
                    OrderNumber = "o1",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    Description = { LanguageUtil.MockAllLanguages("Created list") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Short description") },
                },
            },
            new ProportionalElectionListCreated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = "c995e944-5b49-40f8-a75b-814a10ebc0f0",
                    BlankRowCount = 3,
                    OrderNumber = "o2",
                    Position = 2,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    Description = { LanguageUtil.MockAllLanguages("Created list 2") },
                },
            });

        var list1 = await AdminClient.GetListAsync(new GetProportionalElectionListRequest
        {
            Id = "430f11f8-82bf-4f39-a2b9-d76e8c9dab08",
        });
        var list2 = await AdminClient.GetListAsync(new GetProportionalElectionListRequest
        {
            Id = "c995e944-5b49-40f8-a75b-814a10ebc0f0",
        });
        list1.MatchSnapshot("1");
        list2.MatchSnapshot("2");
    }

    [Fact]
    public async Task TestAggregateUnionLists()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListCreated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = "430f11f8-82bf-4f39-a2b9-d76e8c9dab08",
                    BlankRowCount = 0,
                    OrderNumber = "4",
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                    Description = { LanguageUtil.MockAllLanguages("Liste 4") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Liste 4") },
                },
            });

        (await RunOnDb(db =>
            db.ProportionalElectionUnionLists
                .Where(l => ProportionalElectionUnionMockedData.StGallen1.Id == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.ShortDescription })
                .OrderBy(d => d.OrderNumber)
                .ToListAsync())).MatchSnapshot();
    }

    [Fact]
    public async Task ForeignProportionalElectionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateListAsync(NewValidRequest(l =>
                l.ProportionalElectionId = ProportionalElectionMockedData.IdKircheProportionalElectionInContestKircheWithoutChilds)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MoreBlankRowsThanNumberOfMandatesShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateListAsync(NewValidRequest(o => o.BlankRowCount = 564)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateListAsync(NewValidRequest(o =>
            {
                // this proportional election already has lists, so the list position can't be 1
                o.ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen;
                o.Position = 1;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ListInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.CreateListAsync(NewValidRequest(o =>
            {
                o.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .CreateListAsync(NewValidRequest());

    private CreateProportionalElectionListRequest NewValidRequest(
        Action<CreateProportionalElectionListRequest>? customizer = null)
    {
        var request = new CreateProportionalElectionListRequest
        {
            BlankRowCount = 0,
            OrderNumber = "o1",
            Position = 1,
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
            Description = { LanguageUtil.MockAllLanguages("Created list") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Juso") },
        };

        customizer?.Invoke(request);
        return request;
    }
}
