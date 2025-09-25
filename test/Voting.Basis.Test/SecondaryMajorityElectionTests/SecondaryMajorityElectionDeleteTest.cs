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
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionDeleteTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    private const string IdNotFound = "bfe2cfaf-c787-48b9-a108-c975b0addddd";
    private string? _authTestElectionId;

    public SecondaryMajorityElectionDeleteTest(TestApplicationFactory factory)
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
            async () => await CantonAdminClient.DeleteSecondaryMajorityElectionAsync(new DeleteSecondaryMajorityElectionRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.DeleteSecondaryMajorityElectionAsync(new DeleteSecondaryMajorityElectionRequest
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionDeleted, EventSignatureBusinessMetadata>();

        eventData.SecondaryMajorityElectionId.Should().Be(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund);
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdBundContest);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdBundContest, async () =>
        {
            await CantonAdminClient.DeleteSecondaryMajorityElectionAsync(new DeleteSecondaryMajorityElectionRequest
            {
                Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionDeleted>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new SecondaryMajorityElectionDeleted { SecondaryMajorityElectionId = id });

        var idGuid = Guid.Parse(id);
        (await RunOnDb(db => db.SecondaryMajorityElections.CountAsync(c => c.Id == idGuid)))
            .Should().Be(0);

        await AssertHasPublishedEventProcessedMessage(SecondaryMajorityElectionDeleted.Descriptor, idGuid);
    }

    [Fact]
    public async Task ModificationWithEVotingApprovedShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteSecondaryMajorityElectionAsync(new DeleteSecondaryMajorityElectionRequest
            {
                Id = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionEVotingApprovedInContestStGallen,
            }),
            StatusCode.FailedPrecondition,
            nameof(PoliticalBusinessEVotingApprovedException));
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestElectionId == null)
        {
            var response = await ElectionAdminClient.CreateSecondaryMajorityElectionAsync(new CreateSecondaryMajorityElectionRequest
            {
                PoliticalBusinessNumber = "10246",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Neue Neben-Majorzwahl") },
                Active = true,
                NumberOfMandates = 5,
                PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenWithoutChilds,
            });
            try
            {
                await RunEvents<ElectionGroupCreated>(false);
            }
            catch (DbUpdateException)
            {
                // may already exist
            }

            await RunEvents<SecondaryMajorityElectionCreated>();

            _authTestElectionId = response.Id;
        }

        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .DeleteSecondaryMajorityElectionAsync(new DeleteSecondaryMajorityElectionRequest { Id = _authTestElectionId });
        await RunEvents<SecondaryMajorityElectionDeleted>();
        _authTestElectionId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
