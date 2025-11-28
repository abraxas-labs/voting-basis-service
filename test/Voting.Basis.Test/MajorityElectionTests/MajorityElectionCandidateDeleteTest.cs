// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionCandidateDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestCandidateId;

    public MajorityElectionCandidateDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateDeleted, EventSignatureBusinessMetadata>();

        eventData.MajorityElectionCandidateId.Should().Be(MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await CantonAdminClient.DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionCandidateDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new MajorityElectionCandidateDeleted { MajorityElectionCandidateId = id });

        var candidates = await RunOnDb(db => db.MajorityElectionCandidates
            .Where(c => c.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund))
            .ToListAsync());

        var idGuid = Guid.Parse(id);
        candidates.Where(c => c.Id == idGuid).Should().HaveCount(0);

        // reorder candidates after deletion
        var existingCandidate = candidates.Find(c => c.Id == Guid.Parse(MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund));
        existingCandidate.Should().NotBeNull();
        existingCandidate!.Position.Should().Be(1);
    }

    [Fact]
    public async Task CandidateInContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.CandidateIdGossauMajorityElectionEVotingApprovedInContestStGallen,
            }),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    [Fact]
    public async Task CandidateInBallotGroupShouldThrow()
    {
        var candidateId = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestBund;
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest
            {
                Id = candidateId,
            }),
            StatusCode.FailedPrecondition,
            $"Candidate {candidateId} is in a ballot group");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestCandidateId == null)
        {
            var response = await ElectionAdminClient.CreateCandidateAsync(new CreateMajorityElectionCandidateRequest
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
                PartyShortDescription = { LanguageUtil.MockAllLanguages("SP") },
                PartyLongDescription = { LanguageUtil.MockAllLanguages("SP long description") },
                Title = "title",
                ZipCode = "2000",
                Origin = "origin",
                Street = "street",
                HouseNumber = "1a",
                Country = "CH",
            });
            await RunEvents<MajorityElectionCandidateCreated>();

            _authTestCandidateId = response.Id;
        }

        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .DeleteCandidateAsync(new DeleteMajorityElectionCandidateRequest { Id = _authTestCandidateId });
        _authTestCandidateId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
