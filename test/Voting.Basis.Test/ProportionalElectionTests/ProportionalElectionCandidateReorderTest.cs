// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionCandidateReorderTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionCandidateReorderTest(TestApplicationFactory factory)
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
        await AdminClient.ReorderCandidatesAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidatesReordered, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.ReorderCandidatesAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionCandidatesReordered>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(NewValidEvent());
        await TestEventPublisher.Publish(1, NewValidEvent());

        var candidates = await AdminClient.GetCandidatesAsync(new GetProportionalElectionCandidatesRequest
        {
            ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task ForeignProportionalElectionListShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ReorderCandidatesAsync(NewValidRequest(l =>
                l.ProportionalElectionListId = ProportionalElectionMockedData.ListIdBundProportionalElectionInContestBund)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NonSequentialPositionsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ReorderCandidatesAsync(NewValidRequest(l =>
                l.Orders.Orders[2].Position = 5)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ReorderCandidatesInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.ReorderCandidatesAsync(new ReorderProportionalElectionCandidatesRequest
            {
                ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund,
                Orders = new ProtoModels.EntityOrders
                {
                    Orders =
                {
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
                            Position = 2,
                        },
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
                            Position = 1,
                        },
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.CandidateId2GossauProportionalElectionInContestBund,
                            Position = 3,
                        },
                },
                },
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
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .ReorderCandidatesAsync(NewValidRequest());

    private ReorderProportionalElectionCandidatesRequest NewValidRequest(
        Action<ReorderProportionalElectionCandidatesRequest>? customizer = null)
    {
        var request = new ReorderProportionalElectionCandidatesRequest
        {
            ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
            Orders = new ProtoModels.EntityOrders
            {
                Orders =
                    {
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.CandidateId2GossauDeletedPartyProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                    },
            },
        };

        customizer?.Invoke(request);
        return request;
    }

    private ProportionalElectionCandidatesReordered NewValidEvent()
    {
        return new ProportionalElectionCandidatesReordered
        {
            ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
            CandidateOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.CandidateId2GossauDeletedPartyProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                    },
            },
        };
    }
}
