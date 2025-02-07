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
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestCandidateId;

    public SecondaryMajorityElectionCandidateDeleteTest(TestApplicationFactory factory)
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
            async () => await CantonAdminClient.DeleteSecondaryMajorityElectionCandidateAsync(new DeleteSecondaryMajorityElectionCandidateRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.DeleteSecondaryMajorityElectionCandidateAsync(new DeleteSecondaryMajorityElectionCandidateRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateDeleted, EventSignatureBusinessMetadata>();

        eventData.SecondaryMajorityElectionCandidateId.Should().Be(MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.DeleteSecondaryMajorityElectionCandidateAsync(new DeleteSecondaryMajorityElectionCandidateRequest
            {
                Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidateDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new SecondaryMajorityElectionCandidateDeleted { SecondaryMajorityElectionCandidateId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.SecondaryMajorityElectionCandidates.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task CandidateInBallotGroupShouldThrow()
    {
        var candidateId = MajorityElectionMockedData.SecondaryElectionCandidateId2GossauMajorityElectionInContestBund;
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteSecondaryMajorityElectionCandidateAsync(new DeleteSecondaryMajorityElectionCandidateRequest
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
            var response = await ElectionAdminClient.CreateSecondaryMajorityElectionCandidateAsync(new CreateSecondaryMajorityElectionCandidateRequest
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
            });
            await RunEvents<SecondaryMajorityElectionCandidateCreated>();

            _authTestCandidateId = response.Id;
        }

        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .DeleteSecondaryMajorityElectionCandidateAsync(new DeleteSecondaryMajorityElectionCandidateRequest { Id = _authTestCandidateId });
        _authTestCandidateId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
