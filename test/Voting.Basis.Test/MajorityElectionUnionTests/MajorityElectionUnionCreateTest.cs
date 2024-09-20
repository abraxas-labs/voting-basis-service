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

public class MajorityElectionUnionCreateTest : BaseGrpcTest<MajorityElectionUnionService.MajorityElectionUnionServiceClient>
{
    private int _eventNrCounter;

    public MajorityElectionUnionCreateTest(TestApplicationFactory factory)
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
        var response = await ElectionAdminClient.CreateAsync(NewValidRequest());
        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionUnionCreated, EventSignatureBusinessMetadata>();

        eventData.MatchSnapshot("event", x => x.MajorityElectionUnion.Id);
        eventData.MajorityElectionUnion.Id.Should().Be(response.Id);
        eventMetadata!.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ElectionAdminClient.CreateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionUnionCreated>();
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var newId = Guid.Parse("bb092abb-3f8b-4a06-8169-a738d1369fd0");

        await TestEventPublisher.Publish(
            _eventNrCounter++,
            new MajorityElectionUnionCreated
            {
                MajorityElectionUnion = new MajorityElectionUnionEventData
                {
                    Id = newId.ToString(),
                    Description = "new description",
                    ContestId = ContestMockedData.IdStGallenEvoting,
                },
            });

        var result = await RunOnDb(db => db.MajorityElectionUnions.FirstOrDefaultAsync(u => u.Id == newId));
        result!.MatchSnapshot(x => x.Id);

        await AssertHasPublishedMessage<ContestDetailsChangeMessage>(
            x => x.PoliticalBusinessUnion.HasEqualIdAndNewEntityState(newId, EntityState.Added));
    }

    [Fact]
    public async Task ContestWithNoChildDoiOfMyTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest(x => x.ContestId = ContestMockedData.IdKirche)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task MajorityElectionUnionDisabledShouldThrow()
    {
        await SetCantonSettingsMajorityElectionUnionsEnabled(false);
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "MajorityElection is not enabled for this canton");
    }

    [Fact]
    public async Task InvalidContestIdShouldThrow()
    {
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(
                NewValidRequest(x => x.ContestId = "b4e22024-113b-49ac-8460-2bf1c4a074b1")),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ContestWithEndedTestingPhaseShouldThrow()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastUnlocked);
        await AssertStatus(
            async () => await ElectionAdminClient.CreateAsync(NewValidRequest()),
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
        var response = await new MajorityElectionUnionService.MajorityElectionUnionServiceClient(channel)
            .CreateAsync(NewValidRequest());
        await RunEvents<MajorityElectionUnionCreated>();

        await ElectionAdminClient.DeleteAsync(new DeleteMajorityElectionUnionRequest
        {
            Id = response.Id,
        });
    }

    private CreateMajorityElectionUnionRequest NewValidRequest(
        Action<CreateMajorityElectionUnionRequest>? customizer = null)
    {
        var request = new CreateMajorityElectionUnionRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            Description = "new description",
        };

        customizer?.Invoke(request);
        return request;
    }

    private async Task SetCantonSettingsMajorityElectionUnionsEnabled(bool enabled)
    {
        await TestEventPublisher.Publish(
            _eventNrCounter++,
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
