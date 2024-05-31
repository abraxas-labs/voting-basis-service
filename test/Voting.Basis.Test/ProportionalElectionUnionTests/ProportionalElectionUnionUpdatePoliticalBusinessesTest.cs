// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionUnionTests;

public class ProportionalElectionUnionUpdatePoliticalBusinessesTest : BaseGrpcTest<ProportionalElectionUnionService.ProportionalElectionUnionServiceClient>
{
    public ProportionalElectionUnionUpdatePoliticalBusinessesTest(TestApplicationFactory factory)
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
        await AdminClient.UpdatePoliticalBusinessesAsync(NewValidRequest());
        var events = EventPublisherMock.GetPublishedEvents<ProportionalElectionMandateAlgorithmUpdated>().ToList();
        events = events.OrderBy(x => x.ProportionalElectionId).ToList();
        events.MatchSnapshot("events");
        events.Count.Should().Be(2);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdatePoliticalBusinessesAsync(NewValidRequest());
            return EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionMandateAlgorithmUpdated>().First();
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new ProportionalElectionMandateAlgorithmUpdated
            {
                ProportionalElectionId = id.ToString(),
                MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            });

        var proportionalElection = await RunOnDb(db => db.ProportionalElections.Where(x => x.Id == id).FirstAsync());
        proportionalElection.MandateAlgorithm.Should().Be(ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdatePoliticalBusinessesAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task InvalidMandateAlgorithmByCantonShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdatePoliticalBusinessesAsync(NewValidRequest(o => o.MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum)),
            StatusCode.InvalidArgument,
            "Canton settings does not allow proportional election mandate algorithm");
    }

    [Fact]
    public async Task DuplicateUnionIdsShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdatePoliticalBusinessesAsync(NewValidRequest(x => x.ProportionalElectionUnionIds.Add(ProportionalElectionUnionMockedData.IdStGallen1))),
            StatusCode.InvalidArgument,
            "duplicate union id");
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
            .UpdatePoliticalBusinessesAsync(NewValidRequest());
    }

    private UpdateProportionalElectionUnionPoliticalBusinessesRequest NewValidRequest(
        Action<UpdateProportionalElectionUnionPoliticalBusinessesRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionUnionPoliticalBusinessesRequest
        {
            ProportionalElectionUnionIds = { ProportionalElectionUnionMockedData.IdStGallen1 },
            MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
        };

        customizer?.Invoke(request);
        return request;
    }
}
