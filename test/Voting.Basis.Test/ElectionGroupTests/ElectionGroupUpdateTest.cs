// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ElectionGroupTests;

public class ElectionGroupUpdateTest : BaseGrpcTest<ElectionGroupService.ElectionGroupServiceClient>
{
    public ElectionGroupUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.UpdateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ElectionGroupUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await AdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ElectionGroupUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(NewValidEvent());

        var response = await AdminClient.ListAsync(new ListElectionGroupsRequest
        {
            ContestId = MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.ContestId.ToString(),
        });
        response.MatchSnapshot();

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.ElectionGroup.HasEqualIdAndNewEntityState(Guid.Parse(MajorityElectionMockedData.ElectionGroupIdStGallenMajorityElectionInContestBund), EntityState.Modified));
    }

    [Fact]
    public async Task SiblingDoiWithSameTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(eg =>
            {
                eg.Id = MajorityElectionMockedData.ElectionGroupIdUzwilMajorityElectionInContestStGallen;
                eg.PrimaryMajorityElectionId = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NonMatchingPrimaryElectionIdShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(eg =>
            {
                eg.PrimaryMajorityElectionId = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ElectionGroupInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.ElectionGroupIdGossauMajorityElectionInContestBund;
                o.PrimaryMajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ElectionGroupService.ElectionGroupServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    private UpdateElectionGroupRequest NewValidRequest(
        Action<UpdateElectionGroupRequest>? customizer = null)
    {
        var request = new UpdateElectionGroupRequest
        {
            Id = MajorityElectionMockedData.ElectionGroupIdStGallenMajorityElectionInContestBund,
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            Description = "New description",
        };

        customizer?.Invoke(request);
        return request;
    }

    private ElectionGroupUpdated NewValidEvent(
        Action<ElectionGroupUpdated>? customizer = null)
    {
        var ev = new ElectionGroupUpdated
        {
            ElectionGroupId = MajorityElectionMockedData.ElectionGroupIdStGallenMajorityElectionInContestBund,
            Description = "New description",
        };

        customizer?.Invoke(ev);
        return ev;
    }
}
