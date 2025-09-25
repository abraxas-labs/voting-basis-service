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
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListReorderTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListReorderTest(TestApplicationFactory factory)
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
        await CantonAdminClient.ReorderListsAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListsReordered, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.ReorderListsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListsReordered>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(NewValidEvent());
        await TestEventPublisher.Publish(1, NewValidEvent());

        var lists = await CantonAdminClient.GetListsAsync(new GetProportionalElectionListsRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });
        lists.MatchSnapshot();
    }

    [Fact]
    public async Task NonSequentialPositionsShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.ReorderListsAsync(NewValidRequest(l =>
                l.Orders.Orders[2].Position = 5)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ListsInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.ReorderListsAsync(new ReorderProportionalElectionListsRequest
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
                Orders = new ProtoModels.EntityOrders
                {
                    Orders =
                    {
                            new ProtoModels.EntityOrder
                            {
                                Id = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund,
                                Position = 2,
                            },
                            new ProtoModels.EntityOrder
                            {
                                Id = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund,
                                Position = 1,
                            },
                    },
                },
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.ReorderListsAsync(NewValidRequest(x =>
            {
                x.ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .ReorderListsAsync(NewValidRequest());

    private ReorderProportionalElectionListsRequest NewValidRequest(
        Action<ReorderProportionalElectionListsRequest>? customizer = null)
    {
        var request = new ReorderProportionalElectionListsRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            Orders = new ProtoModels.EntityOrders
            {
                Orders =
                    {
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                        new ProtoModels.EntityOrder
                        {
                            Id = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        };
        customizer?.Invoke(request);
        return request;
    }

    private ProportionalElectionListsReordered NewValidEvent()
    {
        return new ProportionalElectionListsReordered
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            ListOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        };
    }
}
