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

namespace Voting.Basis.Test.MajorityElectionUnionTests;

public class MajorityElectionUnionEntriesUpdateTest : BaseGrpcTest<MajorityElectionUnionService.MajorityElectionUnionServiceClient>
{
    public MajorityElectionUnionEntriesUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnOk()
    {
        await AdminClient.UpdateEntriesAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionUnionEntriesUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateEntriesAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionUnionEntriesUpdated>();
        });
    }

    [Fact]
    public async Task EmptyElectionIdsShouldReturnOk()
    {
        await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.MajorityElectionIds.Clear()));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionUnionEntriesUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionUnionEntriesUpdated
            {
                MajorityElectionUnionEntries = new MajorityElectionUnionEntriesEventData
                {
                    MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
                    MajorityElectionIds =
                    {
                            MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen,
                            MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    },
                },
            });

        var result = await RunOnDb(db =>
            db.MajorityElectionUnionEntries
                .Where(u => u.MajorityElectionUnionId == MajorityElectionUnionMockedData.StGallen1.Id)
                .Select(u => u.MajorityElectionId)
                .OrderBy(id => id)
                .ToListAsync());

        result.MatchSnapshot();
    }

    [Fact]
    public async Task TestProcessorWithEmptyElectionIds()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionUnionEntriesUpdated
            {
                MajorityElectionUnionEntries = new MajorityElectionUnionEntriesEventData
                {
                    MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
                },
            });

        var electionIds = await RunOnDb(db =>
            db.MajorityElectionUnionEntries
                .Where(u => u.MajorityElectionUnionId == MajorityElectionUnionMockedData.StGallen1.Id)
                .Select(u => u.MajorityElectionId)
                .OrderBy(id => id)
                .ToListAsync());

        electionIds.MatchSnapshot("electionIds");
    }

    [Fact]
    public async Task UnionOfDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallenDifferentTenant)),
            StatusCode.PermissionDenied,
            "Only owner of the political business union can edit");
    }

    [Fact]
    public async Task DuplicateElectionIdsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.MajorityElectionIds.Add(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen))),
            StatusCode.InvalidArgument,
            "duplicate political business id");
    }

    [Fact]
    public async Task ElectionIdOfDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.MajorityElectionIds.Add(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen))),
            StatusCode.InvalidArgument,
            "cannot assign a political business from a different tenant or different contest to a political business union");
    }

    [Fact]
    public async Task ElectionIdOfDifferentContestButSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.MajorityElectionIds.Add(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund))),
            StatusCode.InvalidArgument,
            "cannot assign a political business from a different tenant or different contest to a political business union");
    }

    [Fact]
    public async Task InvalidIdShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateEntriesAsync(NewValidRequest(x => x.MajorityElectionUnionId = "b4e22024-113b-49ac-8460-2bf1c4a074b1")),
            StatusCode.NotFound);
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

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionUnionService.MajorityElectionUnionServiceClient(channel)
            .UpdateEntriesAsync(NewValidRequest());
    }

    private UpdateMajorityElectionUnionEntriesRequest NewValidRequest(
        Action<UpdateMajorityElectionUnionEntriesRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionUnionEntriesRequest
        {
            MajorityElectionUnionId = MajorityElectionUnionMockedData.IdStGallen1,
            MajorityElectionIds =
                {
                    MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen,
                    MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                },
        };

        customizer?.Invoke(request);
        return request;
    }
}
