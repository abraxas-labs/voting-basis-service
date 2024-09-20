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

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCandidateReorderTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionCandidateReorderTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.ReorderCandidatesAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidatesReordered, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.ReorderCandidatesAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionCandidatesReordered>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(NewValidEvent());
        await TestEventPublisher.Publish(1, NewValidEvent());

        var candidates = await AdminClient.ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task ForeignMajorityElectionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ReorderCandidatesAsync(NewValidRequest(l =>
                l.MajorityElectionId = MajorityElectionMockedData.IdBundMajorityElectionInContestBund)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NonSequentialPositionsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ReorderCandidatesAsync(NewValidRequest(l =>
                l.Orders.Orders[1].Position = 5)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.ReorderCandidatesAsync(new ReorderMajorityElectionCandidatesRequest
            {
                MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                Orders = new ProtoModels.EntityOrders
                {
                    Orders =
                    {
                            new ProtoModels.EntityOrder
                            {
                                Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
                                Position = 2,
                            },
                            new ProtoModels.EntityOrder
                            {
                                Id = MajorityElectionMockedData.CandidateId2GossauMajorityElectionInContestBund,
                                Position = 1,
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
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .ReorderCandidatesAsync(NewValidRequest());

    private ReorderMajorityElectionCandidatesRequest NewValidRequest(
        Action<ReorderMajorityElectionCandidatesRequest>? customizer = null)
    {
        var request = new ReorderMajorityElectionCandidatesRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen,
            Orders = new ProtoModels.EntityOrders
            {
                Orders =
                    {
                        new ProtoModels.EntityOrder
                        {
                            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestStGallen,
                            Position = 2,
                        },
                        new ProtoModels.EntityOrder
                        {
                            Id = MajorityElectionMockedData.CandidateId2GossauMajorityElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        };

        customizer?.Invoke(request);
        return request;
    }

    private MajorityElectionCandidatesReordered NewValidEvent()
    {
        return new MajorityElectionCandidatesReordered
        {
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen,
            CandidateOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.CandidateId2GossauMajorityElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        };
    }
}
