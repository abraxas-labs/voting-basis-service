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
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
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
                    PartyLongDescription = { LanguageUtil.MockAllLanguages("SVP long updated") },
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
                    ReportingType = SharedProto.MajorityElectionCandidateReportingType.CountToIndividual,
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
        });
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestProcessorShouldUpdateReferences()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateUpdated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                    FirstName = "updated first name",
                    LastName = "updated last name",
                    PoliticalFirstName = "updated pol first name",
                    PoliticalLastName = "updated pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("updated occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("updated occupation title") },
                    DateOfBirth = new DateTime(1961, 5, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 5,
                    Party = { LanguageUtil.MockAllLanguages("SVP") },
                    PartyLongDescription = { LanguageUtil.MockAllLanguages("SVP long updated") },
                    Locality = "updated locality",
                    Number = "updatedNum",
                    Sex = SharedProto.SexType.Male,
                    Title = "updated title",
                    ZipCode = "4321",
                    Origin = "ou",
                    CheckDigit = 0,
                    Street = "updated street",
                    HouseNumber = "u1a",
                    Country = "DE",
                    ReportingType = SharedProto.MajorityElectionCandidateReportingType.CountToIndividual,
                },
            });

        var candidateReference = await RunOnDb(db => db.SecondaryMajorityElectionCandidates.FirstAsync(
            c => c.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund)));
        candidateReference.MatchSnapshot();
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
                    PartyLongDescription = { LanguageUtil.MockAllLanguages("SVP long updated") },
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
                    PartyLongDescription = { LanguageUtil.MockAllLanguages("SVP long updated") },
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
    public async Task UpdateCandidateCreatedAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);

        var createRequest = MajorityElectionCandidateCreateTest.NewValidRequestAfterTestingPhaseEnded();
        var response = await CantonAdminClient.CreateCandidateAsync(createRequest);

        await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(r =>
        {
            r.Id = response.Id;
            r.Number = createRequest.Number;
            r.Position = createRequest.Position;
            r.ReportingType = SharedProto.MajorityElectionCandidateReportingType.CountToIndividual;
        }));

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.Id.Should().Be(response.Id);
        ev.MatchSnapshot("event", r => r.Id);
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(x =>
            {
                x.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
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
                PartyLongDescription = { LanguageUtil.MockAllLanguages("SVP long updated") },
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
    public async Task ReportingTypeInTestingPhaseShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.ReportingType = SharedProto.MajorityElectionCandidateReportingType.Candidate)),
            StatusCode.InvalidArgument,
            "Candidate reporting type cannot be set during testing phase");
    }

    [Fact]
    public async Task NoReportingTypeForCandidateCreatedAfterTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);

        var createRequest = MajorityElectionCandidateCreateTest.NewValidRequestAfterTestingPhaseEnded();
        var response = await CantonAdminClient.CreateCandidateAsync(createRequest);

        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(r =>
            {
                r.Id = response.Id;
                r.Number = createRequest.Number;
                r.Position = createRequest.Position;
            })),
            StatusCode.InvalidArgument,
            "Candidates created after the testing phase must have a reporting type");
    }

    [Fact]
    public async Task NoReportingTypeForCandidateCreatedBeforeTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var request = NewValidRequest();
        await CantonAdminClient.UpdateCandidateAsync(NewValidRequest());

        var (eventData, _) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated, EventSignatureBusinessMetadata>();

        eventData.Id.Should().Be(request.Id);
        eventData.ReportingType.Should().Be(SharedProto.MajorityElectionCandidateReportingType.Unspecified);
    }

    [Fact]
    public async Task ReportingTypeWithIndividualCandidatesDisabledAfterTestingPhaseShouldThrow()
    {
        await SetIndividualCandidatesDisabled();
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);

        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(
                NewValidRequestAfterTestingPhase(x => x.ReportingType = SharedProto.MajorityElectionCandidateReportingType.Candidate)),
            StatusCode.InvalidArgument,
            "Cannot set reporting type if individual candidates are disabled");
    }

    [Fact]
    public async Task NoReportingTypeWithIndividualCandidatesDisabledAfterTestingPhaseShouldWork()
    {
        await SetIndividualCandidatesDisabled();
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var request = NewValidRequestAfterTestingPhase();
        var response = await CantonAdminClient.UpdateCandidateAsync(request);
        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.Id.Should().Be(request.Id);
        ev.ReportingType.Should().Be(SharedProto.MajorityElectionCandidateReportingType.Unspecified);
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
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.PartyLongDescription.Clear())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NotAllPartyLanguagesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateCandidateAsync(NewValidRequest(o => o.PartyShortDescription.Remove(Languages.German))),
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
        var request = NewValidRequestAfterTestingPhase(o =>
        {
            o.PartyShortDescription.Clear();
            o.PartyLongDescription.Clear();
        });
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
            PartyShortDescription = { LanguageUtil.MockAllLanguages("Updated") },
            PartyLongDescription = { LanguageUtil.MockAllLanguages("Updated long desc") },
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Incumbent = true,
            Position = 1,
            Locality = "locality",
            Number = "number1",
            Sex = SharedProto.SexType.Female,
            Title = "title",
            ZipCode = "2000",
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
            PartyShortDescription = { LanguageUtil.MockAllLanguages("PU") },
            PartyLongDescription = { LanguageUtil.MockAllLanguages("PU long desc") },
            Origin = "origin",
            Street = "street updated",
            HouseNumber = "1a updated",
            Country = "CH",
        };

        customizer?.Invoke(request);
        return request;
    }

    private async Task SetIndividualCandidatesDisabled()
    {
        await CantonAdminClient.UpdateAsync(new UpdateMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            ContestId = ContestMockedData.IdBundContest,
            PoliticalBusinessNumber = "5478new",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Majorzwahl Update") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Majorzwahl Update") },
            Active = true,
            AutomaticBallotBundleNumberGeneration = MajorityElectionMockedData.GossauMajorityElectionInContestBund.AutomaticBallotBundleNumberGeneration,
            BallotBundleSize = MajorityElectionMockedData.GossauMajorityElectionInContestBund.BallotBundleSize,
            BallotBundleSampleSize = MajorityElectionMockedData.GossauMajorityElectionInContestBund.BallotBundleSampleSize,
            BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
            MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            NumberOfMandates = MajorityElectionMockedData.GossauMajorityElectionInContestBund.NumberOfMandates,
            ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            IndividualCandidatesDisabled = true,
        });
    }
}
