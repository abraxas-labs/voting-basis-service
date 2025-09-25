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

namespace Voting.Basis.Test.VoteTests;

public class VoteEVotingApprovalUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<VoteService.VoteServiceClient>
{
    public VoteEVotingApprovalUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteEVotingApprovalUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestRevert()
    {
        await ElectionAdminEVotingAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(x => x.Approved = false));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<VoteEVotingApprovalUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task PoliticalBusinessWithMissingEVotingShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
                o.Id = VoteMockedData.IdUzwilVoteInContestStGallen)),
            StatusCode.InvalidArgument,
            "does not support E-Voting");
    }

    [Fact]
    public async Task ContestWithMissingEVotingShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
                o.Id = VoteMockedData.IdBundVoteInContestBund)),
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
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteEVotingApprovalUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen);

        await TestEventPublisher.Publish(
            new VoteEVotingApprovalUpdated
            {
                VoteId = id.ToString(),
                Approved = true,
            });
        var response = await CantonAdminClient.GetAsync(new GetVoteRequest { Id = id.ToString() });
        response.MatchSnapshot();

        await AssertHasPublishedEventProcessedMessage(VoteEVotingApprovalUpdated.Descriptor, id);
    }

    [Fact]
    public async Task VoteInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteEVotingApprovalUpdated>()
            .Should().NotBeNull();
    }

    [Fact]
    public async Task VoteInContestLockedShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Archived);
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
        => await new VoteService.VoteServiceClient(channel)
            .UpdateEVotingApprovalAsync(NewValidRequest());

    private UpdateVoteEVotingApprovalRequest NewValidRequest(
        Action<UpdateVoteEVotingApprovalRequest>? customizer = null)
    {
        var request = new UpdateVoteEVotingApprovalRequest
        {
            Id = VoteMockedData.IdStGallenVoteInContestStGallen,
            Approved = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
