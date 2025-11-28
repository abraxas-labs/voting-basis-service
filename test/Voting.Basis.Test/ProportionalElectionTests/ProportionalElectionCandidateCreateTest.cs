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
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionCandidateCreateTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
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
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest());

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateCreated, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionCandidate.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.ProportionalElectionCandidate.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.CreateCandidateAsync(NewValidRequest());
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
                    ZipCode = "9000",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Origin = "origin",
                    CheckDigit = 6,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
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
                    ZipCode = "9000",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Origin = "origin",
                    CheckDigit = 6,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate1 = await CantonAdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = "420f11f8-82bf-4f39-a2b9-d76e8c9dab08",
        });
        var candidate2 = await CantonAdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = "c885e944-5b49-40f8-a75b-814a10ebc0f0",
        });
        candidate1.MatchSnapshot("1");
        candidate2.MatchSnapshot("2");
    }

    [Fact]
    public async Task TestProcessorWithDeprecatedSexType()
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
                    Sex = SharedProto.SexType.Undefined,
                    Title = "title",
                    ZipCode = "8000",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Origin = "origin",
                    CheckDigit = 6,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = "420f11f8-82bf-4f39-a2b9-d76e8c9dab08",
        });
        candidate.Sex.Should().Be(SharedProto.SexType.Female);
    }

    [Fact]
    public async Task TestProcessorShouldTruncateCandidateNumber()
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
                    Number = "number1toolong",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "5000",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Origin = "origin",
                    CheckDigit = 6,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetProportionalElectionCandidateRequest
        {
            Id = "420f11f8-82bf-4f39-a2b9-d76e8c9dab08",
        });
        candidate.Number.Should().Be("number1too");
    }

    [Fact]
    public async Task DuplicateNumberShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Number = "num1")),
            StatusCode.AlreadyExists,
            "NonUniqueCandidateNumber");
    }

    [Fact]
    public async Task TooOldDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(1820, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task TooYoungDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.DateOfBirth = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp())),
            StatusCode.InvalidArgument,
            "DateOfBirth");
    }

    [Fact]
    public async Task AccumulatedCandidateShouldWork()
    {
        await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o =>
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
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o =>
            {
                // this proportional election list already has candidates, so the position can't be 1
                o.Position = 1;
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TooManyCandidatesShouldThrow()
    {
        await CantonAdminClient.CreateCandidateAsync(NewValidRequest());
        await CantonAdminClient.CreateCandidateAsync(NewValidRequest(c =>
        {
            c.Position = 4;
            c.Number = "n3";
        }));
        await CantonAdminClient.CreateCandidateAsync(NewValidRequest(c =>
        {
            c.Position = 5;
            c.Number = "n4";
        }));

        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(c =>
            {
                c.Position = 6;
                c.Number = "n5";
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TooManyCandidatesWithAccumulationShouldThrow()
    {
        await CantonAdminClient.CreateCandidateAsync(NewValidRequest());
        await CantonAdminClient.CreateCandidateAsync(NewValidRequest(c =>
        {
            c.Position = 4;
            c.Number = "n3";
        }));

        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(c =>
            {
                c.Position = 5;
                c.Accumulated = true;
                c.AccumulatedPosition = 6;
                c.Number = "n4";
            })),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CreateCandidateInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(c =>
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
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.PartyId = DomainOfInfluenceMockedData.PartyIdKirchgemeindeEVP)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task EmptyLocalityShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateLocalityRequired = true);

        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Locality = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyOriginShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateOriginRequired = true);

        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Origin = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyLocalityOnCommunalBusinessShouldWork()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateLocalityRequired = true);

        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o =>
        {
            o.ProportionalElectionListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund;
            o.Locality = string.Empty;
            o.Number = "X";
        }));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyOriginOnCommunalBusinessShouldWork()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => true,
            doi => doi.CantonDefaults.CandidateOriginRequired = true);

        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o =>
        {
            o.ProportionalElectionListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestBund;
            o.Number = "X";
            o.Origin = string.Empty;
        }));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyLocalityOnNonCommunalBusinessShouldWorkWhenOptional()
    {
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Locality = string.Empty));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyOriginOnCommunalNonBusinessShouldWorkWhenOptional()
    {
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Origin = string.Empty));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(x =>
            {
                x.ProportionalElectionListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var response = await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .CreateCandidateAsync(NewValidRequest());
        await RunEvents<ProportionalElectionCandidateCreated>();

        await ElectionAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
        {
            Id = response.Id,
        });
    }

    private CreateProportionalElectionCandidateRequest NewValidRequest(
        Action<CreateProportionalElectionCandidateRequest>? customizer = null)
    {
        var request = new CreateProportionalElectionCandidateRequest
        {
            ProportionalElectionListId = ProportionalElectionMockedData.List1IdStGallenProportionalElectionInContestBund,
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
            Number = "num2",
            Sex = SharedProto.SexType.Female,
            Title = "title",
            ZipCode = "1234",
            PartyId = DomainOfInfluenceMockedData.PartyIdStGallenSVP,
            Origin = "origin",
            Street = "street",
            HouseNumber = "1a",
            Country = "CH",
        };

        customizer?.Invoke(request);
        return request;
    }
}
