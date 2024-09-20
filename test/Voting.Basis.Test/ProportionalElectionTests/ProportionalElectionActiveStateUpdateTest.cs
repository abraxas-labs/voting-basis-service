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
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionActiveStateUpdateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionActiveStateUpdateTest(TestApplicationFactory factory)
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
        await AdminClient.UpdateActiveStateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionActiveStateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateActiveStateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionActiveStateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new ProportionalElectionActiveStateUpdated
            {
                ProportionalElectionId = id.ToString(),
                Active = true,
            });
        var response = await AdminClient.GetAsync(new GetProportionalElectionRequest { Id = id.ToString() });
        response.MatchSnapshot();

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusiness.HasEqualIdAndNewEntityState(id, EntityState.Modified));
    }

    [Fact]
    public async Task ProportionalElectionFromOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateActiveStateAsync(NewValidRequest(o =>
            {
                o.Id = ProportionalElectionMockedData.IdUzwilProportionalElectionInContestBund;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateActiveStateAsync(NewValidRequest(o =>
            {
                o.Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund;
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
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .UpdateActiveStateAsync(NewValidRequest());

    private UpdateProportionalElectionActiveStateRequest NewValidRequest(
        Action<UpdateProportionalElectionActiveStateRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionActiveStateRequest
        {
            Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            Active = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
