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
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReorderTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionCandidateReorderTest(TestApplicationFactory factory)
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
        await AdminClient.ReorderSecondaryMajorityElectionCandidatesAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidatesReordered, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await AdminClient.ReorderSecondaryMajorityElectionCandidatesAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidatesReordered>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(NewValidEvent());
        await TestEventPublisher.Publish(1, NewValidEvent());

        var candidates = await AdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task NonSequentialPositionsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.ReorderSecondaryMajorityElectionCandidatesAsync(NewValidRequest(l =>
                l.Orders.Orders[0].Position = 5)),
            StatusCode.InvalidArgument);
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
            .ReorderSecondaryMajorityElectionCandidatesAsync(NewValidRequest());

    private ReorderSecondaryMajorityElectionCandidatesRequest NewValidRequest(
        Action<ReorderSecondaryMajorityElectionCandidatesRequest>? customizer = null)
    {
        var request = new ReorderSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            Orders = new ProtoModels.EntityOrders(),
        };
        var orders = request.Orders.Orders;
        orders.Add(new ProtoModels.EntityOrder
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
            Position = 2,
        });
        orders.Add(new ProtoModels.EntityOrder
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
            Position = 1,
        });

        customizer?.Invoke(request);
        return request;
    }

    private SecondaryMajorityElectionCandidatesReordered NewValidEvent()
    {
        var ev = new SecondaryMajorityElectionCandidatesReordered
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            CandidateOrders = new EntityOrdersEventData(),
        };
        var orders = ev.CandidateOrders.Orders;
        orders.Add(new EntityOrderEventData
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
            Position = 2,
        });
        orders.Add(new EntityOrderEventData
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
            Position = 1,
        });

        return ev;
    }
}
