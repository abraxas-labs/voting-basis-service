// (c) Copyright by Abraxas Informatik AG
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
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionUnionTests;

public class ProportionalElectionUnionUpdateTest : BaseGrpcTest<ProportionalElectionUnionService.ProportionalElectionUnionServiceClient>
{
    public ProportionalElectionUnionUpdateTest(TestApplicationFactory factory)
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
        await AdminClient.UpdateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUnionUpdated>();
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var id = Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1);

        await TestEventPublisher.Publish(
            new ProportionalElectionUnionUpdated
            {
                ProportionalElectionUnion = new ProportionalElectionUnionEventData
                {
                    Id = id.ToString(),
                    Description = "edited description",
                    ContestId = ContestMockedData.IdStGallenEvoting,
                },
            });

        var result = await RunOnDb(db => db.ProportionalElectionUnions
            .AsSplitQuery()
            .Include(u => u.ProportionalElectionUnionEntries)
            .Include(u => u.ProportionalElectionUnionLists.OrderBy(l => l.OrderNumber))
            .FirstOrDefaultAsync(u => u.Id == id));
        result.MatchSnapshot();

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusinessUnion.HasEqualIdAndNewEntityState(id, EntityState.Modified));
    }

    [Fact]
    public async Task UnionOfDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(x => x.Id = ProportionalElectionUnionMockedData.IdStGallenDifferentTenant)),
            StatusCode.PermissionDenied,
            "Only owner of the political business union can edit");
    }

    [Fact]
    public async Task InvalidIdShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(x => x.Id = "b4e22024-113b-49ac-8460-2bf1c4a074b1")),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(x => x.Id = ProportionalElectionUnionMockedData.IdBund)),
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
        await new ProportionalElectionUnionService.ProportionalElectionUnionServiceClient(channel)
            .UpdateAsync(NewValidRequest());
    }

    private UpdateProportionalElectionUnionRequest NewValidRequest(
        Action<UpdateProportionalElectionUnionRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionUnionRequest
        {
            Id = ProportionalElectionUnionMockedData.IdStGallen1,
            Description = "edited description",
        };

        customizer?.Invoke(request);
        return request;
    }
}
