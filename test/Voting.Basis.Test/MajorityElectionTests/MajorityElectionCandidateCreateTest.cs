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

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCandidateCreateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionCandidateCreateTest(TestApplicationFactory factory)
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
        var response = await AdminClient.CreateCandidateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateCreated, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidate.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.MajorityElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.CreateCandidateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionCandidateCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = "8da8dac5-b4ad-492d-8d7e-0168518103d2",
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Party = { LanguageUtil.MockAllLanguages("Grüne") },
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                },
            },
            new MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = "b21835b0-6c81-4e30-9daa-fc82f3f69f30",
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    Position = 3,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Number = "number1",
                    Party = { LanguageUtil.MockAllLanguages("BDP") },
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                },
            });

        var candidate1 = await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = "8da8dac5-b4ad-492d-8d7e-0168518103d2",
        });
        var candidate2 = await AdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = "b21835b0-6c81-4e30-9daa-fc82f3f69f30",
        });
        candidate1.MatchSnapshot("1");
        candidate2.MatchSnapshot("2");
    }

    [Fact]
    public async Task ForeignMajorityElectionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(l =>
                l.MajorityElectionId = MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche)),
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
    public async Task ContestPastShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateCandidateAsync(NewValidRequest(o =>
            {
                // this majority election list already has candidates, so the position can't be 1
                o.Position = 1;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCandidateInContestWithTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.CreateCandidateAsync(NewValidRequest(o =>
        {
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            o.Position = 3;
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateCreated>();
        eventData.MatchSnapshot("event", c => c.MajorityElectionCandidate.Id);
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .CreateCandidateAsync(NewValidRequest());

    private CreateMajorityElectionCandidateRequest NewValidRequest(
        Action<CreateMajorityElectionCandidateRequest>? customizer = null)
    {
        var request = new CreateMajorityElectionCandidateRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            Position = 2,
            FirstName = "firstName",
            LastName = "lastName",
            PoliticalFirstName = "pol first name",
            PoliticalLastName = "pol last name",
            Occupation = { LanguageUtil.MockAllLanguages("occupation") },
            OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            Incumbent = true,
            Locality = "locality",
            Number = "number2",
            Sex = SharedProto.SexType.Female,
            Party = { LanguageUtil.MockAllLanguages("SP") },
            Title = "title",
            ZipCode = "zip code",
        };

        customizer?.Invoke(request);
        return request;
    }
}
