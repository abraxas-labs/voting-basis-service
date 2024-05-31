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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionUnionTests;

public class ProportionalElectionUnionDeleteTest : BaseGrpcTest<ProportionalElectionUnionService.ProportionalElectionUnionServiceClient>
{
    private string? _authTestUnionId;

    public ProportionalElectionUnionDeleteTest(TestApplicationFactory factory)
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
        await AdminClient.DeleteAsync(new DeleteProportionalElectionUnionRequest
        {
            Id = ProportionalElectionUnionMockedData.IdStGallen1,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionDeleted, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteAsync(new DeleteProportionalElectionUnionRequest
            {
                Id = ProportionalElectionUnionMockedData.IdStGallen1,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUnionDeleted>();
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var id = Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1);

        await TestEventPublisher.Publish(
            new ProportionalElectionUnionDeleted
            {
                ProportionalElectionUnionId = id.ToString(),
            });
        var result = await RunOnDb(db => db.ProportionalElectionUnions.FirstOrDefaultAsync(u => u.Id == id));
        result.Should().BeNull();

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusinessUnion.HasEqualIdAndNewEntityState(id, EntityState.Deleted));
    }

    [Fact]
    public async Task InvalidIdShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteProportionalElectionUnionRequest
            {
                Id = "b4e22024-113b-49ac-8460-2bf1c4a074b1",
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task FromDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteProportionalElectionUnionRequest
            {
                Id = ProportionalElectionUnionMockedData.IdStGallenDifferentTenant,
            }),
            StatusCode.PermissionDenied,
            "Only owner of the political business union can edit");
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteProportionalElectionUnionRequest
            {
                Id = ProportionalElectionUnionMockedData.IdBund,
            }),
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
    {
        if (_authTestUnionId == null)
        {
            var response = await ElectionAdminClient.CreateAsync(new CreateProportionalElectionUnionRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                Description = "new description",
            });
            await RunEvents<ProportionalElectionUnionCreated>();

            _authTestUnionId = response.Id;
        }

        await new ProportionalElectionUnionService.ProportionalElectionUnionServiceClient(channel)
            .DeleteAsync(new DeleteProportionalElectionUnionRequest
            {
                Id = _authTestUnionId,
            });
        _authTestUnionId = null;
    }
}
