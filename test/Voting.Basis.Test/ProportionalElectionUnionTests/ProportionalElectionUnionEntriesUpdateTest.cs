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
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionUnionTests;

public class ProportionalElectionUnionEntriesUpdateTest : BaseGrpcTest<ProportionalElectionUnionService.ProportionalElectionUnionServiceClient>
{
    public ProportionalElectionUnionEntriesUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnOk()
    {
        await AdminClient.UpdateEntriesAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionEntriesUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateEntriesAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUnionEntriesUpdated>();
        });
    }

    [Fact]
    public async Task EmptyElectionIdsShouldReturnOk()
    {
        await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.ProportionalElectionIds.Clear()));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionEntriesUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionUnionEntriesUpdated
            {
                ProportionalElectionUnionEntries = new ProportionalElectionUnionEntriesEventData
                {
                    ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
                    ProportionalElectionIds =
                    {
                            ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen,
                            ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                    },
                },
            });

        var electionIds = await RunOnDb(db =>
            db.ProportionalElectionUnionEntries
                .Where(u => u.ProportionalElectionUnionId == ProportionalElectionUnionMockedData.StGallen1.Id)
                .Select(u => u.ProportionalElectionId)
                .OrderBy(id => id)
                .ToListAsync());

        var unionLists = await ElectionAdminClient.GetProportionalElectionUnionListsAsync(new GetProportionalElectionUnionListsRequest
        {
            ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
        });

        electionIds.MatchSnapshot("electionIds");
        unionLists.MatchSnapshot("unionLists");
    }

    [Fact]
    public async Task TestProcessorWithEmptyElectionIds()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionUnionEntriesUpdated
            {
                ProportionalElectionUnionEntries = new ProportionalElectionUnionEntriesEventData
                {
                    ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
                },
            });

        var electionIds = await RunOnDb(db =>
            db.ProportionalElectionUnionEntries
                .Where(u => u.ProportionalElectionUnionId == ProportionalElectionUnionMockedData.StGallen1.Id)
                .Select(u => u.ProportionalElectionId)
                .OrderBy(id => id)
                .ToListAsync());

        var unionLists = await ElectionAdminClient.GetProportionalElectionUnionListsAsync(new GetProportionalElectionUnionListsRequest
        {
            ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
        });

        electionIds.MatchSnapshot("electionIds");
        unionLists.MatchSnapshot("unionLists");
    }

    [Fact]
    public async Task UnionOfDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallenDifferentTenant)),
            StatusCode.PermissionDenied,
            "Only owner of the political business union can edit");
    }

    [Fact]
    public async Task DuplicateElectionIdsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.ProportionalElectionIds.Add(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen))),
            StatusCode.InvalidArgument,
            "duplicate political business id");
    }

    [Fact]
    public async Task MultipleMandateAlgorithmsInElectionsShouldThrow()
    {
        await ModifyDbEntities<ProportionalElection>(
            pe => pe.Id == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen),
            pe => pe.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum);

        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Only proportional elections with the same mandate algorithms may be combined");
    }

    [Fact]
    public async Task ElectionIdOfDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.ProportionalElectionIds.Add(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen))),
            StatusCode.InvalidArgument,
            "cannot assign a political business from a different tenant or different contest to a political business union");
    }

    [Fact]
    public async Task ElectionIdOfDifferentContestButSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.ProportionalElectionIds.Add(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund))),
            StatusCode.InvalidArgument,
            "cannot assign a political business from a different tenant or different contest to a political business union");
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task InvalidIdShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.ProportionalElectionUnionId = "b4e22024-113b-49ac-8460-2bf1c4a074b1")),
            StatusCode.NotFound);
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionService.ProportionalElectionUnionServiceClient(channel)
            .UpdateEntriesAsync(NewValidRequest());
    }

    private UpdateProportionalElectionUnionEntriesRequest NewValidRequest(
        Action<UpdateProportionalElectionUnionEntriesRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionUnionEntriesRequest
        {
            ProportionalElectionUnionId = ProportionalElectionUnionMockedData.IdStGallen1,
            ProportionalElectionIds =
                {
                    ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                    ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                },
        };

        customizer?.Invoke(request);
        return request;
    }
}
