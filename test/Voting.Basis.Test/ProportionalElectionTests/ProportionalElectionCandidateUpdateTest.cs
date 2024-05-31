﻿// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionCandidateUpdateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionCandidateUpdateTest(TestApplicationFactory factory)
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
        await AdminClient.UpdateCandidateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.ProportionalElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateCandidateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionCandidateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionCandidateUpdated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    PartyId = DomainOfInfluenceMockedData.PartyIdStGallenSP,
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidate = await AdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
        });
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestProcessorDeletedParty()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionCandidateUpdated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    PartyId = DomainOfInfluenceMockedData.PartyIdGossauDeleted,
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidate = await AdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
        });
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task DeletedPartyShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(x =>
            {
                x.Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen;
                x.ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen;
                x.PartyId = DomainOfInfluenceMockedData.PartyIdGossauDeleted;
            })),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task SameDeletedPartyAsBeforeShouldWork()
    {
        await AdminClient.UpdateCandidateAsync(NewValidRequest(x =>
        {
            x.Id = ProportionalElectionMockedData.CandidateId2GossauDeletedPartyProportionalElectionInContestStGallen;
            x.ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen;
            x.PartyId = DomainOfInfluenceMockedData.PartyIdGossauDeleted;
            x.Position = 3;
            x.Accumulated = false;
            x.AccumulatedPosition = 0;
            x.Number = "number2";
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateUpdated>();
        eventData.MatchSnapshot("event", d => d.ProportionalElectionCandidate.Id);
    }

    [Fact]
    public async Task ForeignProportionalElectionCandidateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(l =>
                l.Id = ProportionalElectionMockedData.CandidateIdKircheProportionalElectionInContestKirche)),
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
    public async Task AccumulatedCandidateShouldWork()
    {
        await AdminClient.UpdateCandidateAsync(NewValidRequest(o =>
        {
            o.Accumulated = true;
            o.AccumulatedPosition = 2;
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task RemoveAccumulationShouldWork()
    {
        await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Accumulated = false));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Position = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ForeignPartyShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o => o.PartyId = DomainOfInfluenceMockedData.PartyIdKirchgemeindeEVP)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ProportionalElectionCandidateUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);

        await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase());

        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await AdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
        });
        election.MatchSnapshot("response");
    }

    [Fact]
    public async Task ProportionalElectionCandidateUpdateAfterTestingPhaseWithEmptyLocalityShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x =>
            {
                x.Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen;
                x.ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen;
                x.Locality = string.Empty;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionCandidateUpdateAfterTestingPhaseWithEmptyOriginShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x =>
            {
                x.Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen;
                x.ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen;
                x.Origin = string.Empty;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task ProportionalElectionCandidateUpdateAfterTestingPhaseWithEmptyLocalityOnCommunalBusinessShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x => x.Locality = string.Empty));

        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await AdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
        });
        election.MatchSnapshot("response");
    }

    [Fact]
    public async Task ProportionalElectionCandidateUpdateAfterTestingPhaseWithEmptyOriginOnCommunalBusinessShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase(x => x.Origin = string.Empty));

        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var election = await AdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
        });
        election.MatchSnapshot("response");
    }

    [Fact]
    public async Task ProportionalElectionCandidateUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o =>
            {
                o.Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund;
                o.ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund;
                o.Number = "newnumber";
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task ProportionalElectionCandidateInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.UpdateCandidateAsync(NewValidRequest(o =>
            {
                o.Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund;
                o.ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund;
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
            o.Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund;
            o.ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund;
            o.Locality = string.Empty;
        });
        await AdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.ProportionalElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task EmptyOriginOnCommunalBusinessShouldWork()
    {
        var request = NewValidRequest(o =>
        {
            o.Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund;
            o.ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund;
            o.Origin = string.Empty;
        });
        await AdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event", d => d.ProportionalElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
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
            .UpdateCandidateAsync(NewValidRequest());

    private UpdateProportionalElectionCandidateRequest NewValidRequest(
        Action<UpdateProportionalElectionCandidateRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
            ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
            FirstName = "new first name",
            LastName = "new last name",
            PoliticalFirstName = "pol first name",
            PoliticalLastName = "pol last name",
            Occupation = { LanguageUtil.MockAllLanguages("occupation") },
            OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Incumbent = true,
            Position = 1,
            Accumulated = true,
            AccumulatedPosition = 2,
            Locality = "locality",
            Number = "number1",
            Sex = SharedProto.SexType.Female,
            Title = "title",
            ZipCode = "zip code",
            PartyId = DomainOfInfluenceMockedData.PartyIdStGallenSP,
            Origin = "origin",
        };

        customizer?.Invoke(request);
        return request;
    }

    private UpdateProportionalElectionCandidateRequest NewValidRequestAfterTestingPhase(
        Action<UpdateProportionalElectionCandidateRequest>? customizer = null)
    {
        var request = new UpdateProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
            ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestBund,
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
            Accumulated = true,
            AccumulatedPosition = 2,
            PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
            Origin = "origin",
        };

        customizer?.Invoke(request);
        return request;
    }
}
