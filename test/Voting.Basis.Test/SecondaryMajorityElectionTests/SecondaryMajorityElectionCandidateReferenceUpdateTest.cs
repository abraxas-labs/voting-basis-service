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
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionCandidateReferenceUpdateTest(TestApplicationFactory factory)
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
        await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestOnlyChangeIncumbentShouldWork()
    {
        // This shouldn't throw
        var response = await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(x =>
        {
            x.Number = "number1"; // candidate already has this number
            x.Incumbent = false;
        }));
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidateReferenceUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceUpdated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                    SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                    Incumbent = true,
                    Position = 1,
                    Number = "2.1",
                    CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit("2.1"),
                    ReportingType = SharedProto.MajorityElectionCandidateReportingType.CountToIndividual,
                },
            });

        var candidates = await CantonAdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(o =>
            {
                o.Position = 5;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DuplicateNumberShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(o => o.Number = "number2")),
            StatusCode.AlreadyExists,
            "NonUniqueCandidateNumber");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(x =>
            {
                x.Id = MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionEVotingApprovedInContestStGallen;
                x.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    [Fact]
    public async Task ReportingTypeInTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(
                NewValidRequest(x => x.ReportingType = SharedProto.MajorityElectionCandidateReportingType.Candidate)),
            StatusCode.InvalidArgument,
            "Candidate reporting type cannot be set during testing phase");
    }

    [Fact]
    public async Task UpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequestAfterTestingPhaseEnded());

        var ev = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceUpdated>();
        ev.MatchSnapshot("event");
    }

    [Fact]
    public async Task UpdateCandidateCreatedAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);

        var createRequest = SecondaryMajorityElectionCandidateReferenceCreateTest.NewValidRequest(x => x.ReportingType = SharedProto.MajorityElectionCandidateReportingType.Candidate);
        var response = await CantonAdminClient.CreateMajorityElectionCandidateReferenceAsync(createRequest);

        await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(r =>
        {
            r.Id = response.Id;
            r.CandidateId = createRequest.CandidateId;
            r.SecondaryMajorityElectionId = createRequest.SecondaryMajorityElectionId;
            r.Number = createRequest.Number;
            r.Position = createRequest.Position;
            r.ReportingType = SharedProto.MajorityElectionCandidateReportingType.CountToIndividual;
        }));

        var ev = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceUpdated>();
        ev.MajorityElectionCandidateReference.Id.Should().Be(response.Id);
        ev.MatchSnapshot("event", r => r.MajorityElectionCandidateReference.Id);
    }

    [Fact]
    public async Task UpdateCandidateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(o =>
            {
                o.Number = "new number";
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task NoReportingTypeForCandidateCreatedAfterTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);

        var createRequest = SecondaryMajorityElectionCandidateReferenceCreateTest.NewValidRequest(x => x.ReportingType = SharedProto.MajorityElectionCandidateReportingType.Candidate);
        var response = await CantonAdminClient.CreateMajorityElectionCandidateReferenceAsync(createRequest);

        await AssertStatus(
            async () => await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(r =>
            {
                r.Id = response.Id;
                r.CandidateId = createRequest.CandidateId;
                r.SecondaryMajorityElectionId = createRequest.SecondaryMajorityElectionId;
                r.Number = createRequest.Number;
                r.Position = createRequest.Position;
            })),
            StatusCode.InvalidArgument,
            "Candidates created after the testing phase must have a reporting type");
    }

    [Fact]
    public async Task NoReportingTypeForCandidateCreatedBeforeTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var request = NewValidRequestAfterTestingPhaseEnded();
        await CantonAdminClient.UpdateMajorityElectionCandidateReferenceAsync(request);

        var (eventData, _) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidateReference.Id.Should().Be(request.Id);
        eventData.MajorityElectionCandidateReference.ReportingType.Should().Be(SharedProto.MajorityElectionCandidateReportingType.Unspecified);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest());
        await RunEvents<SecondaryMajorityElectionCandidateReferenceUpdated>();

        await ElectionAdminClient.UpdateMajorityElectionCandidateReferenceAsync(NewValidRequest(x => x.Number = "number1"));
    }

    private UpdateMajorityElectionCandidateReferenceRequest NewValidRequest(
        Action<UpdateMajorityElectionCandidateReferenceRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionCandidateReferenceRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
            Incumbent = true,
            Number = "3.2",
            Position = 1,
        };

        customizer?.Invoke(request);
        return request;
    }

    private UpdateMajorityElectionCandidateReferenceRequest NewValidRequestAfterTestingPhaseEnded(
        Action<UpdateMajorityElectionCandidateReferenceRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionCandidateReferenceRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
            Incumbent = true,
            Number = "number1",
            Position = 1,
        };

        customizer?.Invoke(request);
        return request;
    }
}
