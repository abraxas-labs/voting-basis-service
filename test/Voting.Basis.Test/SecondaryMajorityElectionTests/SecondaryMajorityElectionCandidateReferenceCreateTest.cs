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
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceCreateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionCandidateReferenceCreateTest(TestApplicationFactory factory)
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
        var response = await CantonAdminClient.CreateMajorityElectionCandidateReferenceAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceCreated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidateReference.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidateReference.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.CreateMajorityElectionCandidateReferenceAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidateReferenceCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceCreated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = "f6cbf48c-47b9-4275-b782-b9222a2c3ea1",
                    SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    Position = 3,
                    Number = "9.1",
                    CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit("9.1"),
                    Incumbent = false,
                    CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                },
            });

        var candidates = await CantonAdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task ContestLockedShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Archived);
        await AssertStatus(
            async () => await CantonAdminClient.CreateMajorityElectionCandidateReferenceAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateMajorityElectionCandidateReferenceAsync(NewValidRequest(o =>
            {
                // this secondary majority eection already has candidates, so the position can't be 1
                o.Position = 1;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCandidateReferenceInContestWithTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await CantonAdminClient.CreateMajorityElectionCandidateReferenceAsync(NewValidRequest(x =>
        {
            x.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
            x.CandidateId = MajorityElectionMockedData.CandidateId2GossauMajorityElectionInContestBund;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceCreated>();
        eventData.MatchSnapshot("event", e => e.MajorityElectionCandidateReference.Id);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var response = await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .CreateMajorityElectionCandidateReferenceAsync(NewValidRequest());
        await RunEvents<SecondaryMajorityElectionCandidateReferenceCreated>();

        await ElectionAdminClient.DeleteMajorityElectionCandidateReferenceAsync(new DeleteMajorityElectionCandidateReferenceRequest
        {
            Id = response.Id,
        });
    }

    private CreateMajorityElectionCandidateReferenceRequest NewValidRequest(
        Action<CreateMajorityElectionCandidateReferenceRequest>? customizer = null)
    {
        var request = new CreateMajorityElectionCandidateReferenceRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            Position = 3,
            Incumbent = false,
            Number = "1.2",
            CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
        };

        customizer?.Invoke(request);
        return request;
    }
}
