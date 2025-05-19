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
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCandidateUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
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
        var response = await CantonAdminClient.UpdateCandidateAsync(request);

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
            await CantonAdminClient.UpdateCandidateAsync(NewValidRequest());
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
                    Street = "new street",
                    HouseNumber = "new 1a",
                    Country = "CH",
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
        });
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestProcessorWithDeprecatedSexType()
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
                    Sex = SharedProto.SexType.Undefined,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Origin = "origin",
                    CheckDigit = 0,
                    Street = "new street",
                    HouseNumber = "new 1a",
                    Country = "CH",
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
        });
        candidate.Sex.Should().Be(SharedProto.SexType.Female);
    }

    [Fact]
    public async Task TestProcessorShouldTruncateCandidateNumber()
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
                    Number = "numberNewtoolong",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Origin = "origin",
                    CheckDigit = 0,
                    Street = "new street",
                    HouseNumber = "new 1a",
                    Country = "CH",
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
        });
        candidate.Number.Should().Be("numberNewt");
    }

    [Fact]
    public async Task ForeignMajorityElectionCandidateShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(l =>
                l.Id = MajorityElectionMockedData.CandidateIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TooOldDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(1820, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task TooYoungDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Position = 2)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await CantonAdminClient.UpdateCandidateAsync(NewValidRequestAfterTestingPhase());

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
        });
        candidate.MatchSnapshot("response");
    }

    [Fact]
    public async Task TestProcessorUpdateAfterTestingPhaseWithDeprecatedSexType()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateAfterTestingPhaseUpdated
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
                Party = { LanguageUtil.MockAllLanguages("SVP") },
                Locality = "locality",
                Sex = SharedProto.SexType.Undefined,
                Title = "new title",
                ZipCode = "new zip code",
                Origin = "origin",
                Street = "new street",
                HouseNumber = "new 1a",
                Country = "CH",
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
        });
        candidate.Sex.Should().Be(SharedProto.SexType.Female);
    }

    [Fact]
    public async Task MajorityElectionCandidateUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund;
                o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
                o.Number = "new number";
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task CandidateInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o =>
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
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateLocalityRequired = true);

        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Locality = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyOriginShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateOriginRequired = true);

        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Origin = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyPartyShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Party.Clear())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NotAllPartyLanguagesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Party.Remove(Languages.German))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.DateOfBirth = null)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptySexShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.Sex = SharedProto.SexType.Unspecified)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyLocalityOnCommunalBusinessShouldWork()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateLocalityRequired = true);

        var request = NewValidRequest(o =>
        {
            o.Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau;
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;
            o.Locality = string.Empty;
        });
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdGossau);
    }

    [Fact]
    public async Task EmptyOriginOnCommunalBusinessShouldWork()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateOriginRequired = true);

        var request = NewValidRequest(o =>
        {
            o.Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau;
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;
            o.Origin = string.Empty;
        });
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdGossau);
    }

    [Fact]
    public async Task EmptyLocalityOnNonCommunalBusinessShouldWorkWhenOptional()
    {
        var request = NewValidRequest(o => o.Locality = string.Empty);
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
    }

    [Fact]
    public async Task EmptyOriginOnNonCommunalBusinessShouldWorkWhenOptional()
    {
        var request = NewValidRequest(o => o.Origin = string.Empty);
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
    }

    [Fact]
    public async Task EmptyDateOfBirthAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var request = NewValidRequestAfterTestingPhase(o => o.DateOfBirth = null);
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, _) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated, EventSignatureBusinessMetadata>();

        eventData.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.Id);
    }

    [Fact]
    public async Task EmptySexAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var request = NewValidRequestAfterTestingPhase(o => o.Sex = SharedProto.SexType.Unspecified);
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, _) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated, EventSignatureBusinessMetadata>();

        eventData.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.Id);
    }

    [Fact]
    public async Task EmptyPartyAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var request = NewValidRequestAfterTestingPhase(o => o.Party.Clear());
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, _) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated, EventSignatureBusinessMetadata>();

        eventData.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.Id);
    }

    [Fact]
    public async Task EmptyLocalityAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var request = NewValidRequestAfterTestingPhase(o => o.Locality = string.Empty);
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, _) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated, EventSignatureBusinessMetadata>();

        eventData.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.Id);
    }

    [Fact]
    public async Task EmptyOriginAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var request = NewValidRequestAfterTestingPhase(o => o.Origin = string.Empty);
        await CantonAdminClient.UpdateCandidateAsync(request);
        var (eventData, _) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated, EventSignatureBusinessMetadata>();

        eventData.Id.Should().Be(request.Id);
        eventData.MatchSnapshot("event", d => d.Id);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
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
            Street = "street",
            HouseNumber = "1a",
            Country = "CH",
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
            Street = "street updated",
            HouseNumber = "1a updated",
            Country = "CH",
        };

        customizer?.Invoke(request);
        return request;
    }
}
