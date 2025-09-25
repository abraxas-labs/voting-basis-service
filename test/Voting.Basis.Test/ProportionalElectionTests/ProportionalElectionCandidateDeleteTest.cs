// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
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
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionCandidateDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";
    private string? _authTestCandidateId;

    public ProportionalElectionCandidateDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestInvalidGuid()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
        {
            Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCandidateDeleted, EventSignatureBusinessMetadata>();

        eventData.ProportionalElectionCandidateId.Should().Be(ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionCandidateDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen;
        await TestEventPublisher.Publish(new ProportionalElectionCandidateDeleted { ProportionalElectionCandidateId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.ProportionalElectionCandidates.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task DeleteCandidateInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await CantonAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest
            {
                Id = ProportionalElectionMockedData.CandidateIdGossauProportionalElectionEVotingApprovedInContestStGallen,
            }),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestCandidateId == null)
        {
            var response = await ElectionAdminClient.CreateCandidateAsync(new CreateProportionalElectionCandidateRequest
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
                ZipCode = "2000",
                PartyId = DomainOfInfluenceMockedData.PartyIdStGallenSVP,
                Origin = "origin",
                Street = "street",
                HouseNumber = "1a",
                Country = "CH",
            });
            await RunEvents<ProportionalElectionCandidateCreated>();

            _authTestCandidateId = response.Id;
        }

        await new ProportionalElectionService.ProportionalElectionServiceClient(channel)
            .DeleteCandidateAsync(new DeleteProportionalElectionCandidateRequest { Id = _authTestCandidateId });
        _authTestCandidateId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
