// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateUpdateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionCandidateUpdateTest(TestApplicationFactory factory)
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
        await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidateUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateUpdated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 8, 6, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Party = { LanguageUtil.MockAllLanguages("NEW") },
                    Origin = "origin",
                    CheckDigit = 0,
                },
            });

        var candidates = await AdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task ForeignSecondaryMajorityElectionCandidateShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(l =>
                l.Id = MajorityElectionMockedData.SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TooOldDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(1820, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task TooYoungDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o =>
            {
                o.Position = 5;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task SecondaryMajorityElectionCandidateUpdateAfterTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequestAfterTestingPhase());

        var ev = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var candidates = await AdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
        });
        var candidate = candidates.Candidates
            .Single(c => c.Candidate.Id == MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund)
            .Candidate;
        candidate.MatchSnapshot("response");
    }

    [Fact]
    public async Task SecondaryMajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyLocalityShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequestAfterTestingPhase(x =>
            {
                x.Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund;
                x.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund;
                x.Locality = string.Empty;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task SecondaryMajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyOriginShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequestAfterTestingPhase(x =>
            {
                x.Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund;
                x.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund;
                x.Origin = string.Empty;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task SecondaryMajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyLocalityOnCommunalBusinessShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequestAfterTestingPhase(x => x.Locality = string.Empty));

        var ev = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var candidates = await AdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
        });
        var candidate = candidates.Candidates
            .Single(c => c.Candidate.Id == MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund)
            .Candidate;
        candidate.MatchSnapshot("response");
    }

    [Fact]
    public async Task SecondaryMajorityElectionCandidateUpdateAfterTestingPhaseWithEmptyOriginOnCommunalBusinessShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequestAfterTestingPhase(x => x.Origin = string.Empty));

        var ev = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated>();
        ev.MatchSnapshot("event");

        await TestEventPublisher.Publish(ev);
        var candidates = await AdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
        });
        var candidate = candidates.Candidates
            .Single(c => c.Candidate.Id == MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund)
            .Candidate;
        candidate.MatchSnapshot("response");
    }

    [Fact]
    public async Task SecondaryMajorityElectionCandidateUpdateAfterTestingPhaseShouldRestrictSomeFields()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund;
                o.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
                o.Number = "new number";
            })),
            StatusCode.FailedPrecondition,
            "ModificationNotAllowedException: Some modifications are not allowed because the testing phase has ended.");
    }

    [Fact]
    public async Task SecondaryCandidateInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o =>
            {
                o.Id = MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestBund;
                o.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
            })),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task EmptyLocalityShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.Locality = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyOriginShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.Origin = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyLocalityOnCommunalBusinessShouldWork()
    {
        var request = NewValidRequest(o =>
        {
            o.Id = MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund;
            o.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
            o.Locality = string.Empty;
        });
        await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task EmptyOriginOnCommunalBusinessShouldWork()
    {
        var request = NewValidRequest(o =>
        {
            o.Id = MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund;
            o.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
            o.Origin = string.Empty;
        });
        await AdminClient.UpdateSecondaryMajorityElectionCandidateAsync(request);
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateSecondaryMajorityElectionCandidateAsync(NewValidRequest());

    private static UpdateSecondaryMajorityElectionCandidateRequest NewValidRequest(
        Action<UpdateSecondaryMajorityElectionCandidateRequest>? customizer = null)
    {
        var request = new UpdateSecondaryMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            FirstName = "new first name",
            LastName = "new last name",
            PoliticalFirstName = "pol first name",
            PoliticalLastName = "pol last name",
            Occupation = { LanguageUtil.MockAllLanguages("occupation") },
            OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Incumbent = true,
            Position = 2,
            Locality = "locality",
            Number = "number56",
            Sex = SharedProto.SexType.Female,
            Title = "title",
            ZipCode = "zip code",
            Party = { LanguageUtil.MockAllLanguages("FDP") },
            Origin = "origin",
        };

        customizer?.Invoke(request);
        return request;
    }

    private static UpdateSecondaryMajorityElectionCandidateRequest NewValidRequestAfterTestingPhase(
        Action<UpdateSecondaryMajorityElectionCandidateRequest>? customizer = null)
    {
        var request = new UpdateSecondaryMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund,
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
            Number = "number2",
            FirstName = "update first name",
            LastName = "updated last name",
            PoliticalFirstName = "updated p. first name",
            PoliticalLastName = "updated p. last name",
            Position = 2,
            DateOfBirth = new DateTime(1990, 4, 5, 7, 45, 3, DateTimeKind.Utc).ToTimestamp(),
            Locality = "locality updated",
            Sex = SharedProto.SexType.Male,
            Occupation = { LanguageUtil.MockAllLanguages("occupation updated") },
            Party = { LanguageUtil.MockAllLanguages("PU") },
            Incumbent = true,
            Origin = "origin",
        };

        customizer?.Invoke(request);
        return request;
    }
}
