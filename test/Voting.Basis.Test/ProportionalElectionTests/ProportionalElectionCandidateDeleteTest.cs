// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionCandidateDeleteTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public ProportionalElectionCandidateDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestInvalidGuid()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateDeleted, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionCandidateId.Should().Be(ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionCandidateDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionCandidateDeleted { ProportionalElectionCandidateId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.ProportionalElectionCandidates.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task ProportionalElectionOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestParentProportionalElectionShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = ProportionalElectionMockedData.CandidateId1BundProportionalElectionInContestStGallen,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DeleteCandidateInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var id = ProportionalElectionMockedData.CandidateIdGossauProportionalElectionInContestGossau;

        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest { Id = id });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
