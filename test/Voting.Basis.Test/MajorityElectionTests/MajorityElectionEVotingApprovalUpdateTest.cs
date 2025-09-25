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
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MajorityElectionTests;

public class MajorityElectionEVotingApprovalUpdateTest : PoliticalBusinessAuthorizationGrpcBaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionEVotingApprovalUpdateTest(TestApplicationFactory factory)
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
        await ElectionAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEVotingApprovalUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestRevert()
    {
        await ElectionAdminEVotingAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(x => x.Approved = false));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEVotingApprovalUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task PoliticalBusinessWithMissingEVotingShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
                o.Id = MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen)),
            StatusCode.InvalidArgument,
            "does not support E-Voting");
    }

    [Fact]
    public async Task ContestWithMissingEVotingShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
                o.Id = MajorityElectionMockedData.IdBundMajorityElectionInContestBund)),
            StatusCode.FailedPrecondition,
            nameof(ContestMissingEVotingException));
    }

    [Fact]
    public async Task TestRevertWithoutRevertPermissionShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateEVotingApprovalAsync(NewValidRequest(o =>
            {
                o.Approved = false;
            })),
            StatusCode.PermissionDenied,
            "Cannot revert E-Voting approval");
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ElectionAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionEVotingApprovalUpdated>();
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new MajorityElectionEVotingApprovalUpdated
            {
                MajorityElectionId = id.ToString(),
                Approved = true,
            });
        var response = await ElectionAdminClient.GetAsync(new GetMajorityElectionRequest { Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen });
        response.MatchSnapshot();

        await AssertHasPublishedEventProcessedMessage(MajorityElectionEVotingApprovalUpdated.Descriptor, id);
    }

    [Fact]
    public async Task MajorityElectionInContestWithEndedTestingPhaseShouldWork()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastUnlocked);
        await ElectionAdminClient.UpdateEVotingApprovalAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEVotingApprovalUpdated>()
            .Should().NotBeNull();
    }

    [Fact]
    public async Task MajorityElectionInContestLockedShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ElectionAdminClient.UpdateEVotingApprovalAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            nameof(ContestLockedException));
    }

    [Fact]
    public async Task SetActiveWithInvalidBallotGroupVoteCountsShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == Guid.Parse(ContestMockedData.IdBundContest),
            c => c.EVoting = true);

        await CantonAdminClient.CreateBallotGroupAsync(new CreateMajorityElectionBallotGroupRequest
        {
            Description = "test new",
            Position = 3,
            ShortDescription = "short - long",
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
            Entries =
                {
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        ElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                        Id = MajorityElectionMockedData.BallotGroupEntryId21GossauMajorityElectionInContestBund,
                    },
                    new ProtoModels.MajorityElectionBallotGroupEntry
                    {
                        ElectionId = MajorityElectionMockedData.SecondaryElectionIdGossauMajorityElectionInContestBund,
                        Id = MajorityElectionMockedData.BallotGroupEntryId22GossauMajorityElectionInContestBund,
                    },
                },
        });

        await AssertStatus(
            async () => await ElectionAdminClient.UpdateEVotingApprovalAsync(new UpdateMajorityElectionEVotingApprovalRequest
            {
                Id = MajorityElectionMockedData.IdGossauMajorityElectionInContestBund,
                Approved = true,
            }),
            StatusCode.FailedPrecondition,
            nameof(MajorityElectionBallotGroupVoteCountException));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .UpdateEVotingApprovalAsync(NewValidRequest());

    private UpdateMajorityElectionEVotingApprovalRequest NewValidRequest(
        Action<UpdateMajorityElectionEVotingApprovalRequest>? customizer = null)
    {
        var request = new UpdateMajorityElectionEVotingApprovalRequest
        {
            Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            Approved = true,
        };

        customizer?.Invoke(request);
        return request;
    }
}
