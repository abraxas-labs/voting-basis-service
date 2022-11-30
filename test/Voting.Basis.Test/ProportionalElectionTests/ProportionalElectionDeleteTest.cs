// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionDeleteTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";

    public ProportionalElectionDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteAsync(new DeleteProportionalElectionRequest
        {
            Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionDeleted, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionId.Should().Be(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionDeleted { ProportionalElectionId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.ProportionalElections.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(idGuid, EntityState.Deleted));
    }

    [Fact]
    public async Task TestAggregateUnionLists()
    {
        var id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionDeleted { ProportionalElectionId = id });

        (await RunOnDb(db =>
            db.ProportionalElectionUnionLists
                .Where(l => ProportionalElectionUnionMockedData.StGallen1.Id == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.ShortDescription })
                .OrderBy(x => x.OrderNumber)
                .ToListAsync())).MatchSnapshot();
    }

    [Fact]
    public async Task ProportionalElectionOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestParentProportionalElectionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = ProportionalElectionMockedData.IdBundProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteProportionalElectionRequest
            {
                Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau;

        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .DeleteAsync(new DeleteProportionalElectionRequest { Id = id });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
