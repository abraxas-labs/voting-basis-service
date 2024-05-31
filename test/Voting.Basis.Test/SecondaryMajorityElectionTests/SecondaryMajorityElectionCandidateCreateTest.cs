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
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateCreateTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public SecondaryMajorityElectionCandidateCreateTest(TestApplicationFactory factory)
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
        var response = await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateCreated, EventSignatureBusinessMetadata>();

        eventData.SecondaryMajorityElectionCandidate.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.SecondaryMajorityElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidateCreated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateCreated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = "5b8ca432-50d3-464f-abcf-37d51fa22b3b",
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
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
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("SP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            },
            new SecondaryMajorityElectionCandidateCreated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = "78caaa44-741f-4cc9-8dbe-1361ed2d8662",
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    Position = 4,
                    FirstName = "first",
                    LastName = "last",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1961, 1, 28, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Male,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("CVP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidates = await AdminClient.ListSecondaryMajorityElectionCandidatesAsync(new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task ContestLockedShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastLocked);
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task ForeignSecondaryMajorityElectionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(c =>
                c.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DuplicateNumberShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.Number = "number1")),
            StatusCode.AlreadyExists,
            "NonUniqueCandidateNumber");
    }

    [Fact]
    public async Task TooOldDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(1820, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task TooYoungDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o =>
            {
                // this secondary majority election already has candidates, so the position can't be 1
                o.Position = 1;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCandidateInContestWithTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(x =>
            x.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateCreated>();
        eventData.MatchSnapshot("event", d => d.SecondaryMajorityElectionCandidate.Id);
    }

    [Fact]
    public async Task EmptyLocalityShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.Locality = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyOriginShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o => o.Origin = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyLocalityOnCommunalBusinessShouldWork()
    {
        var response = await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o =>
        {
            o.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
            o.Locality = string.Empty;
        }));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyOriginOnCommunalBusinessShouldWork()
    {
        var response = await AdminClient.CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest(o =>
        {
            o.SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund;
            o.Origin = string.Empty;
        }));
        response.Id.Should().NotBeNull();
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var response = await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .CreateSecondaryMajorityElectionCandidateAsync(NewValidRequest());
        await RunEvents<SecondaryMajorityElectionCandidateCreated>();

        await ElectionAdminClient.DeleteSecondaryMajorityElectionCandidateAsync(
            new DeleteSecondaryMajorityElectionCandidateRequest
            {
                Id = response.Id,
            });
    }

    private static CreateSecondaryMajorityElectionCandidateRequest NewValidRequest(
        Action<CreateSecondaryMajorityElectionCandidateRequest>? customizer = null)
    {
        var request = new CreateSecondaryMajorityElectionCandidateRequest
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
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
            Number = "number24",
            Sex = SharedProto.SexType.Female,
            Title = "title",
            ZipCode = "zip code",
            Party = { LanguageUtil.MockAllLanguages("DFP") },
            Origin = "origin",
        };

        customizer?.Invoke(request);
        return request;
    }
}
