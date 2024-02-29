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
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListUnionDeleteTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";

    public ProportionalElectionListUnionDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
        {
            Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListUnionDeleted, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionListUnionId.Should().Be(ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListUnion2IdGossauProportionalElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListUnionDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen;

        await TestEventPublisher.Publish(new ProportionalElectionListUnionDeleted { ProportionalElectionListUnionId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.ProportionalElectionListUnions.CountAsync(c => c.Id == idGuid || c.ProportionalElectionRootListUnionId == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task ProportionalElectionOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListIdUzwilProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestParentProportionalElectionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListIdBundProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ListUnionInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
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
            async () => await ElectionAdminClient.DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest
            {
                Id = ProportionalElectionMockedData.ListUnionIdGossauProportionalElectionInContestBund,
            }),
            StatusCode.InvalidArgument,
            "The election does not distribute mandates per Hagenbach-Bischoff algorithm");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var id = ProportionalElectionMockedData.ListUnion1IdGossauProportionalElectionInContestStGallen;

        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .DeleteListUnionAsync(new DeleteProportionalElectionListUnionRequest { Id = id });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
