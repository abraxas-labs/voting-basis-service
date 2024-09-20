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
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceDeleteTest : BaseGrpcTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestCandidateReferenceId;

    public SecondaryMajorityElectionCandidateReferenceDeleteTest(TestApplicationFactory factory)
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
            async () => await AdminClient.DeleteMajorityElectionCandidateReferenceAsync(new DeleteMajorityElectionCandidateReferenceRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.DeleteMajorityElectionCandidateReferenceAsync(new DeleteMajorityElectionCandidateReferenceRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestStGallen,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCandidateReferenceDeleted, EventSignatureBusinessMetadata>();

        eventData.SecondaryMajorityElectionCandidateReferenceId.Should().Be(MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestStGallen);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteMajorityElectionCandidateReferenceAsync(new DeleteMajorityElectionCandidateReferenceRequest
            {
                Id = MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestStGallen,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionCandidateReferenceDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new SecondaryMajorityElectionCandidateDeleted { SecondaryMajorityElectionCandidateId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.SecondaryMajorityElectionCandidates.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);
    }

    [Fact]
    public async Task SecondaryMajorityElectionOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteMajorityElectionCandidateReferenceAsync(new DeleteMajorityElectionCandidateReferenceRequest
            {
                Id = MajorityElectionMockedData.SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CandidateInBallotGroupShouldThrow()
    {
        var candidateId = MajorityElectionMockedData.SecondaryElectionCandidateId1GossauMajorityElectionInContestBund;
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteMajorityElectionCandidateReferenceAsync(new DeleteMajorityElectionCandidateReferenceRequest
            {
                Id = candidateId,
            }),
            StatusCode.FailedPrecondition,
            $"Candidate {candidateId} is in a ballot group");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestCandidateReferenceId == null)
        {
            var response = await ElectionAdminClient.CreateMajorityElectionCandidateReferenceAsync(new CreateMajorityElectionCandidateReferenceRequest
            {
                SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                Position = 3,
                Incumbent = false,
                CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
            });
            await RunEvents<SecondaryMajorityElectionCandidateReferenceCreated>();

            _authTestCandidateReferenceId = response.Id;
        }

        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .DeleteMajorityElectionCandidateReferenceAsync(new DeleteMajorityElectionCandidateReferenceRequest { Id = _authTestCandidateReferenceId });
        _authTestCandidateReferenceId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
