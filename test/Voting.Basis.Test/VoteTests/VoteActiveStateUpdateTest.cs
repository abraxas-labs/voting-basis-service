// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.VoteTests;

public class VoteActiveStateUpdateTest : BaseGrpcTest<VoteService.VoteServiceClient>
{
    public VoteActiveStateUpdateTest(TestApplicationFactory factory)
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
        await AdminClient.UpdateActiveStateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteActiveStateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateActiveStateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteActiveStateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen);

        await TestEventPublisher.Publish(
            new VoteActiveStateUpdated
            {
                VoteId = id.ToString(),
                Active = true,
            });
        var response = await AdminClient.GetAsync(new GetVoteRequest { Id = id.ToString() });
        response.MatchSnapshot();

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id, EntityState.Modified));
    }

    [Fact]
    public async Task VoteFromOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateActiveStateAsync(NewValidRequest(o =>
            {
                o.Id = VoteMockedData.IdUzwilVoteInContestBund;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VoteInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateActiveStateAsync(NewValidRequest(o =>
            {
                o.Id = VoteMockedData.IdGossauVoteInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new VoteService.VoteServiceClient(channel)
            .UpdateActiveStateAsync(NewValidRequest());

    private UpdateVoteActiveStateRequest NewValidRequest(
        Action<UpdateVoteActiveStateRequest>? customizer = null)
    {
        var request = new UpdateVoteActiveStateRequest
        {
            Id = VoteMockedData.IdStGallenVoteInContestStGallen,
            Active = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
