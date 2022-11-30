// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionCandidateCreateTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionCandidateCreateTest(TestApplicationFactory factory)
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
        var response = await AdminClient.CreateCandidateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateCreated, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionCandidate.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateCandidateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionCandidateCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionCandidateCreated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = "420f11f8-82bf-4f39-a2b9-d76e8c9dab08",
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                },
            },
            new ProportionalElectionCandidateCreated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = "c885e944-5b49-40f8-a75b-814a10ebc0f0",
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                },
            });

        var candidate1 = await AdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = "420f11f8-82bf-4f39-a2b9-d76e8c9dab08",
        });
        var candidate2 = await AdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = "c885e944-5b49-40f8-a75b-814a10ebc0f0",
        });
        candidate1.MatchSnapshot("1");
        candidate2.MatchSnapshot("2");
    }

    [Fact]
    public async Task ForeignProportionalElectionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(l =>
                l.ProportionalElectionListId = ProportionalElectionMockedData.ListIdKircheProportionalElectionInContestKirche)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DuplicateNumberShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(o => o.Number = "number1")),
            StatusCode.AlreadyExists,
            "NonUniqueCandidateNumber");
    }

    [Fact]
    public async Task TooOldDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(1820, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task TooYoungDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task AccumulatedCandidateShouldWork()
    {
        await AdminClient.CreateCandidateAsync(NewValidRequest(o =>
        {
            o.Accumulated = true;
            o.AccumulatedPosition = 4;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateCreated>();
        eventData.MatchSnapshot("event", c => c.ProportionalElectionCandidate.Id);
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(o =>
            {
                // this proportional election list already has candidates, so the position can't be 1
                o.Position = 1;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TooManyCandidatesShouldThrow()
    {
        await AdminClient.CreateCandidateAsync(NewValidRequest());
        await AdminClient.CreateCandidateAsync(NewValidRequest(c =>
        {
            c.Position = 4;
            c.Number = "n3";
        }));

        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(c =>
            {
                c.Position = 5;
                c.Number = "n4";
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TooManyCandidatesWithAccumulationShouldThrow()
    {
        await AdminClient.CreateCandidateAsync(NewValidRequest());

        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(c =>
            {
                c.Position = 4;
                c.Accumulated = true;
                c.AccumulatedPosition = 5;
                c.Number = "n4";
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCandidateInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(c =>
            {
                c.ProportionalElectionListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund;
                c.Position = 1;
            })),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ForeignPartyShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(o => o.PartyId = DomainOfInfluenceMockedData.PartyIdKirchgemeindeEVP)),
            StatusCode.NotFound);
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .CreateCandidateAsync(NewValidRequest());

    private CreateProportionalElectionCandidateRequest NewValidRequest(
        Action<CreateProportionalElectionCandidateRequest>? customizer = null)
    {
        var request = new CreateProportionalElectionCandidateRequest
        {
            ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
            Position = 3,
            FirstName = "firstName",
            LastName = "lastName",
            PoliticalFirstName = "pol first name",
            PoliticalLastName = "pol last name",
            Occupation = { LanguageUtil.MockAllLanguages("occupation") },
            OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Incumbent = true,
            Accumulated = false,
            Locality = "locality",
            Number = "number2",
            Sex = SharedProto.SexType.Female,
            Title = "title",
            ZipCode = "zip code",
            PartyId = DomainOfInfluenceMockedData.PartyIdStGallenSVP,
        };

        customizer?.Invoke(request);
        return request;
    }
}
