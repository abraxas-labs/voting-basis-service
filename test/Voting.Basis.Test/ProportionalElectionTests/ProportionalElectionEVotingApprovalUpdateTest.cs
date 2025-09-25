// (c) Copyright by Abraxas Informatik AG
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
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionEVotingApprovalUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionEVotingApprovalUpdateTest(TestApplicationFactory factory)
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
        await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEVotingApprovalUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestRevert()
    {
        await ElectionAdminEVotingAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(x => x.Approved = false));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEVotingApprovalUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task PoliticalBusinessWithMissingEVotingShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
                o.Id = ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen)),
            StatusCode.InvalidArgument,
            "does not support E-Voting");
    }

    [Fact]
    public async Task ContestWithMissingEVotingShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
                o.Id = ProportionalElectionMockedData.IdBundProportionalElectionInContestBund)),
            StatusCode.FailedPrecondition,
            nameof(ContestMissingEVotingException));
    }

    [Fact]
    public async Task TestRevertWithoutRevertPermissionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
            {
                o.Approved = false;
            })),
            StatusCode.PermissionDenied,
            "Cannot revert E-Voting approval");
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionEVotingApprovalUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new ProportionalElectionEVotingApprovalUpdated
            {
                ProportionalElectionId = id.ToString(),
                Approved = true,
            });
        var response = await CantonAdminClient.GetAsync(new GetProportionalElectionRequest { Id = id.ToString() });
        response.MatchSnapshot();

        await AssertHasPublishedEventProcessedMessage(ProportionalElectionEVotingApprovalUpdated.Descriptor, id);
    }

    [Fact]
    public async Task ProportionalElectionInContestWithEndedTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastUnlocked);
        await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEVotingApprovalUpdated>()
            .Should().NotBeNull();
    }

    [Fact]
    public async Task ProportionalElectionInContestLockedShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            nameof(ContestLockedException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .UpdateEVotingApprovalAsync(NewValidRequest());

    private UpdateProportionalElectionEVotingApprovalRequest NewValidRequest(
        Action<UpdateProportionalElectionEVotingApprovalRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionEVotingApprovalRequest
        {
            Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            Approved = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
