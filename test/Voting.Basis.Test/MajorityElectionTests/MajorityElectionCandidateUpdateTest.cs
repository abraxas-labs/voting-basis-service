// (c) Copyright 2024 by Abraxas Informatik AG
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
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCandidateUpdateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionCandidateUpdateTest(TestApplicationFactory factory)
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
        var request = NewValidRequest();
        var response = await AdminClient.UpdateCandidateAsync(request);

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateCandidateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionCandidateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateUpdated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Party = { LanguageUtil.MockAllLanguages("SVP") },
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Origin = "origin",
                    CheckDigit = 0,
                },
            });

        var candidate = await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
        });
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task ForeignMajorityElectionCandidateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(l =>
                l.Id = MajorityElectionMockedData.CandidateIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TooOldDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(1820, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task TooYoungDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Position = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase());

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var candidate = await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
        });
        candidate.MatchSnapshot("response");
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyLocalityShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x =>
            {
                x.Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen;
                x.MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen;
                x.Locality = string.Empty;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyOriginShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x =>
            {
                x.Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen;
                x.MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen;
                x.Origin = string.Empty;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyLocalityOnCommunalBusinessShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x => x.Locality = string.Empty));

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var candidate = await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
        });
        candidate.MatchSnapshot("response");
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyOriginOnCommunalBusinessShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x => x.Origin = string.Empty));

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var candidate = await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
        });
        candidate.MatchSnapshot("response");
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund;
                o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
                o.Number = "new number";
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task CandiateInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund;
                o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task EmptyLocalityShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Locality = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyOriginShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Origin = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyLocalityOnCommunalBusinessShouldWork()
    {
        var request = NewValidRequest(o =>
        {
            o.Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau;
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;
            o.Locality = string.Empty;
        });
        await AdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdGossau);
    }

    [Fact]
    public async Task EmptyOriginOnCommunalBusinessShouldWork()
    {
        var request = NewValidRequest(o =>
        {
            o.Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau;
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;
            o.Origin = string.Empty;
        });
        await AdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdGossau);
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateCandidateAsync(NewValidRequest());

    private UpdateMajorityElectionCandidateRequest NewValidRequest(
        Action<UpdateMajorityElectionCandidateRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            FirstName = "new first name",
            LastName = "new last name",
            PoliticalFirstName = "pol first name",
            PoliticalLastName = "pol last name",
            Occupation = { LanguageUtil.MockAllLanguages("occupation") },
            OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
            Party = { LanguageUtil.MockAllLanguages("Test") },
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Incumbent = true,
            Position = 1,
            Locality = "locality",
            Number = "number1",
            Sex = SharedProto.SexType.Female,
            Title = "title",
            ZipCode = "zip code",
            Origin = "origin",
        };

        customizer?.Invoke(request);
        return request;
    }

    private UpdateMajorityElectionCandidateRequest NewValidRequestAfterTestingPhase(
        Action<UpdateMajorityElectionCandidateRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            Number = "number1",
            FirstName = "update first name",
            LastName = "updated last name",
            PoliticalFirstName = "updated p. first name",
            PoliticalLastName = "updated p. last name",
            Position = 1,
            DateOfBirth = new DateTime(1990, 4, 5, 7, 45, 3, DateTimeKind.Utc).ToTimestamp(),
            Locality = "locality updated",
            Sex = SharedProto.SexType.Male,
            Occupation = { LanguageUtil.MockAllLanguages("occupation updated") },
            Party = { LanguageUtil.MockAllLanguages("PU") },
            Origin = "origin",
        };

        customizer?.Invoke(request);
        return request;
    }
}
