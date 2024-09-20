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
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MajorityElectionUnionTests;

public class MajorityElectionUnionDeleteTest : BaseGrpcTest<MajorityElectionUnionService.MajorityElectionUnionServiceClient>
{
    private string? _authTestUnionId;

    public MajorityElectionUnionDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await MajorityElectionMockedData.Seed(RunScoped);
        await SetCantonSettingsMajorityElectionUnionsEnabled(true);
    }

    [Fact]
    public async Task TestShouldReturnOk()
    {
        await AdminClient.DeleteAsync(new DeleteMajorityElectionUnionRequest
        {
            Id = MajorityElectionUnionMockedData.IdStGallen1,
        });
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionUnionDeleted, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot("event");
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.DeleteAsync(new DeleteMajorityElectionUnionRequest
            {
                Id = MajorityElectionUnionMockedData.IdStGallen1,
            });
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionUnionDeleted>();
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var id = Guid.Parse(MajorityElectionUnionMockedData.IdStGallen1);

        await TestEventPublisher.Publish(
            new MajorityElectionUnionDeleted
            {
                MajorityElectionUnionId = id.ToString(),
            });
        var result = await RunOnDb(db => db.MajorityElectionUnions.FirstOrDefaultAsync(u => u.Id == id));
        result.Should().BeNull();

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusinessUnion.HasEqualIdAndNewEntityState(id, EntityState.Deleted));
    }

    [Fact]
    public async Task InvalidIdShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteMajorityElectionUnionRequest
            {
                Id = "b4e22024-113b-49ac-8460-2bf1c4a074b1",
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task FromDifferentTenantShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteMajorityElectionUnionRequest
            {
                Id = MajorityElectionUnionMockedData.IdStGallenDifferentTenant,
            }),
            StatusCode.PermissionDenied,
            "Only owner of the political business union can edit");
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundContest, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.DeleteAsync(new DeleteMajorityElectionUnionRequest
            {
                Id = MajorityElectionUnionMockedData.IdBund,
            }),
            StatusCode.FailedPrecondition,
            "Testing phase ended, cannot modify the contest");
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
        if (_authTestUnionId == null)
        {
            var response = await ElectionAdminClient.CreateAsync(new CreateMajorityElectionUnionRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                Description = "new description",
            });
            await RunEvents<MajorityElectionUnionCreated>();

            _authTestUnionId = response.Id;
        }

        await new MajorityElectionUnionService.MajorityElectionUnionServiceClient(channel)
            .DeleteAsync(new DeleteMajorityElectionUnionRequest
            {
                Id = _authTestUnionId,
            });
        _authTestUnionId = null;
    }

    private async Task SetCantonSettingsMajorityElectionUnionsEnabled(bool enabled)
    {
        await TestEventPublisher.Publish(
            0,
            new CantonSettingsUpdated
            {
                CantonSettings = new CantonSettingsEventData
                {
                    Id = CantonSettingsMockedData.IdStGallen,
                    Canton = SharedProto.DomainOfInfluenceCanton.Sg,
                    AuthorityName = "St.Gallen",
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                    ProportionalElectionMandateAlgorithms =
                    {
                        SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    },
                    MajorityElectionAbsoluteMajorityAlgorithm =
                        SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
                    MajorityElectionInvalidVotes = false,
                    SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.SeparateCountingCircle,
                    SwissAbroadVotingRightDomainOfInfluenceTypes =
                    {
                        SharedProto.DomainOfInfluenceType.Ch,
                    },
                    EnabledPoliticalBusinessUnionTypes =
                    {
                        enabled
                            ? SharedProto.PoliticalBusinessUnionType.PoliticalBusinessUnionMajorityElection
                            : SharedProto.PoliticalBusinessUnionType.PoliticalBusinessUnionProportionalElection,
                    },
                },
            });
    }
}
