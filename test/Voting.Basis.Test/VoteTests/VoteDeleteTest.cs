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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.VoteTests;

public class VoteDeleteTest : BaseGrpcTest<VoteService.VoteServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";

    public VoteDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await VoteMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteAsync(new DeleteVoteRequest
        {
            Id = VoteMockedData.IdStGallenVoteInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<VoteDeleted, EventSignatureBusinessMetadata>();

        eventData.VoteId.Should().Be(VoteMockedData.IdStGallenVoteInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = VoteMockedData.IdStGallenVoteInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = VoteMockedData.IdStGallenVoteInContestStGallen;
        await TestEventPublisher.Publish(new VoteDeleted { VoteId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.Votes.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(idGuid, EntityState.Deleted));
    }

    [Fact]
    public async Task VoteOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = VoteMockedData.IdUzwilVoteInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestParentVoteShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = VoteMockedData.IdBundVoteInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VoteInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteVoteRequest
            {
                Id = VoteMockedData.IdGossauVoteInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var id = VoteMockedData.IdGossauVoteInContestGossau;

        await new VoteService.VoteServiceClient(channel)
            .DeleteAsync(new DeleteVoteRequest { Id = id });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
