﻿// (c) Copyright by Abraxas Informatik AG
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
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionReorderTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListUnionReorderTest(TestApplicationFactory factory)
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
        await AdminClient.ReorderListUnionsAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionsReordered, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.ReorderListUnionsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionsReordered>();
        });
    }

    [Fact]
    public async Task TestAggregateListUnion()
    {
        await TestEventPublisher.Publish(NewValidEventListUnion());
        await TestEventPublisher.Publish(1, NewValidEventListUnion());

        var tree = await AdminClient.GetListUnionsAsync(new GetProportionalElectionListUnionsRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });
        tree.MatchSnapshot();
    }

    [Fact]
    public async Task TestAggregateSubListUnion()
    {
        await TestEventPublisher.Publish(NewValidEventSubListUnion());
        await TestEventPublisher.Publish(1, NewValidEventSubListUnion());

        var tree = await AdminClient.GetListUnionsAsync(new GetProportionalElectionListUnionsRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });
        tree.MatchSnapshot();
    }

    [Fact]
    public async Task NonSequentialPositionsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ReorderListUnionsAsync(NewValidRequest(l =>
                l.Orders.Orders[2].Position = 4)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.ReorderListUnionsAsync(new ReorderProportionalElectionListUnionsRequest
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
                ProportionalElectionRootListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ListUnionInNonHagenbachBischoffElectionShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.Id == DomainOfInfluenceMockedData.GuidGossau,
            doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms = new()
            {
                ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            });

        await ElectionAdminClient.UpdateAsync(new()
        {
            Id = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
            PoliticalBusinessNumber = "1",
            NumberOfMandates = 2,
            MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            ContestId = ContestMockedData.IdBundContest,
            ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
        });

        await AssertStatus(
            async () => await ElectionAdminClient.ReorderListUnionsAsync(new()
            {
                ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestBund,
                ProportionalElectionRootListUnionId = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
            }),
            StatusCode.InvalidArgument,
            "The election does not distribute mandates per Hagenbach-Bischoff algorithm");
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
            .ReorderListUnionsAsync(NewValidRequest());

    private ReorderProportionalElectionListUnionsRequest NewValidRequest(
        Action<ReorderProportionalElectionListUnionsRequest>? customizer = null)
    {
        var request = new ReorderProportionalElectionListUnionsRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            Orders = new ProtoModels.EntityOrders(),
        };
        var orders = request.Orders.Orders;
        orders.Add(new ProtoModels.EntityOrder
        {
            Id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
            Position = 2,
        });
        orders.Add(new ProtoModels.EntityOrder
        {
            Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
            Position = 3,
        });
        orders.Add(new ProtoModels.EntityOrder
        {
            Id = ProportionalElectionMockedData.ListUnion3IdGossauProportionalElectionInContestStGallen,
            Position = 1,
        });

        customizer?.Invoke(request);
        return request;
    }

    private ProportionalElectionListUnionsReordered NewValidEventListUnion()
    {
        return new ProportionalElectionListUnionsReordered
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            ProportionalElectionListUnionOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
                            Position = 3,
                        },
                        new EntityOrderEventData
                        {
                            Id = ProportionalElectionMockedData.ListUnion3IdGossauProportionalElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        };
    }

    private ProportionalElectionListUnionsReordered NewValidEventSubListUnion()
    {
        var ev = new ProportionalElectionListUnionsReordered
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            ProportionalElectionRootListUnionId = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
            ProportionalElectionListUnionOrders = new EntityOrdersEventData(),
        };
        var orders = ev.ProportionalElectionListUnionOrders.Orders;
        orders.Add(new EntityOrderEventData
        {
            Id = ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen,
            Position = 2,
        });
        orders.Add(new EntityOrderEventData
        {
            Id = ProportionalElectionMockedData.SubListUnion22IdGossauProportionalElectionInContestStGallen,
            Position = 1,
        });

        return ev;
    }
}
