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
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCandidateCreateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
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
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest());

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
            await CantonAdminClient.CreateCandidateAsync(NewValidRequest());
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
                    ZipCode = "9000",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
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
                    ZipCode = "9000",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate1 = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = "8da8dac5-b4ad-492d-8d7e-0168518103d2",
        });
        var candidate2 = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = "b21835b0-6c81-4e30-9daa-fc82f3f69f30",
        });
        candidate1.MatchSnapshot("1");
        candidate2.MatchSnapshot("2");
    }

    [Fact]
    public async Task DuplicateNumberShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Number = "number1")),
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
    public async Task InvalidSwissZipCodeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.ZipCode = "test")),
            StatusCode.InvalidArgument,
            "ZipCode");
    }

    [Fact]
    public async Task ContestPastShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task NonContinuousPositionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o =>
            {
                // this majority election list already has candidates, so the position can't be 1
                o.Position = 1;
            })),
            StatusCode.InvalidArgument);
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
    public async Task EmptyPartyShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Party.Clear())),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task NotAllPartyLanguagesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Party.Remove(Languages.German))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptyDateOfBirthShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.DateOfBirth = null)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task EmptySexShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Sex = SharedProto.SexType.Unspecified)),
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
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;
            o.Locality = string.Empty;
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
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau;
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
    public async Task EmptyOriginOnNonCommunalBusinessShouldWorkWhenOptional()
    {
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Origin = string.Empty));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyDateOfBirthAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.DateOfBirth = null));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptySexAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Sex = SharedProto.SexType.Unspecified));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyPartyAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Party.Clear()));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyLocalityAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Locality = string.Empty));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task EmptyOriginAfterTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.Active);
        var response = await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o => o.Origin = string.Empty));
        response.Id.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCandidateInContestWithTestingPhaseEndedShouldWork()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await CantonAdminClient.CreateCandidateAsync(NewValidRequest(o =>
        {
            o.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund;
            o.Position = 3;
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateCreated>();
        eventData.MatchSnapshot("event", c => c.MajorityElectionCandidate.Id);
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateCandidateAsync(NewValidRequest(x =>
            {
                x.MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionEVotingApprovedInContestStGallen;
            })),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    [Fact]
    public async Task TestProcessorWithDeprecatedSexType()
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
                    Sex = SharedProto.SexType.Undefined,
                    Title = "title",
                    ZipCode = "9000",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = "8da8dac5-b4ad-492d-8d7e-0168518103d2",
        });
        candidate.Sex.Should().Be(SharedProto.SexType.Female);
    }

    [Fact]
    public async Task TestProcessorShouldTruncateCandidateNumber()
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
                    Number = "number1toolong",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "9000",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate = await CantonAdminClient.GetCandidateAsync(new GetMajorityElectionCandidateRequest
        {
            Id = "8da8dac5-b4ad-492d-8d7e-0168518103d2",
        });

        candidate.Number.Should().Be("number1too");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var response = await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .CreateCandidateAsync(NewValidRequest());
        await RunEvents<MajorityElectionCandidateCreated>();

        await ElectionAdminClient.DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest
        {
            Id = response.Id,
        });
    }

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
            ZipCode = "9000",
            Origin = "origin",
            Street = "street",
            HouseNumber = "1a",
            Country = "CH",
        };

        customizer?.Invoke(request);
        return request;
    }
}
