// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using ServiceModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.CantonSettingsTests;

public class CantonSettingsUpdateTest : BaseGrpcTest<CantonSettingsService.CantonSettingsServiceClient>
{
    public CantonSettingsUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task Test()
    {
        await AdminClient.UpdateAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CantonSettingsUpdated>();
        eventData.MatchSnapshot("event", c => c.CantonSettings.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new CantonSettingsUpdated
            {
                CantonSettings = new CantonSettingsEventData
                {
                    Id = CantonSettingsMockedData.IdStGallen,
                    Canton = SharedProto.DomainOfInfluenceCanton.Sg,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    AuthorityName = "St.Gallen Update",
                    ProportionalElectionMandateAlgorithms =
                    {
                            SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                            SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum,
                    },
                    MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
                    MajorityElectionInvalidVotes = false,
                    SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.OnEveryCountingCircle,
                    SwissAbroadVotingRightDomainOfInfluenceTypes =
                    {
                            SharedProto.DomainOfInfluenceType.Ch,
                            SharedProto.DomainOfInfluenceType.Ki,
                            SharedProto.DomainOfInfluenceType.Ct,
                            SharedProto.DomainOfInfluenceType.Ko,
                            SharedProto.DomainOfInfluenceType.Mu,
                    },
                    EnabledPoliticalBusinessUnionTypes =
                    {
                            SharedProto.PoliticalBusinessUnionType.PoliticalBusinessUnionMajorityElection,
                    },
                    EnabledVotingCardChannels =
                    {
                            new CantonSettingsVotingCardChannelEventData
                            {
                                Valid = true,
                                VotingChannel = SharedProto.VotingChannel.ByMail,
                            },
                            new CantonSettingsVotingCardChannelEventData
                            {
                                Valid = false,
                                VotingChannel = SharedProto.VotingChannel.ByMail,
                            },
                            new CantonSettingsVotingCardChannelEventData
                            {
                                Valid = true,
                                VotingChannel = SharedProto.VotingChannel.BallotBox,
                            },
                    },
                    VotingDocumentsEVotingEaiMessageType = "1234567",
                    ProtocolDomainOfInfluenceSortType = SharedProto.ProtocolDomainOfInfluenceSortType.Alphabetical,
                    ProtocolCountingCircleSortType = SharedProto.ProtocolCountingCircleSortType.SortNumber,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var cantonSettingsList = await AdminClient.ListAsync(new ListCantonSettingsRequest());
        cantonSettingsList.CantonSettingsList_.Should().HaveCount(2);
        cantonSettingsList.MatchSnapshot("cantonSettingsList");

        var affectedDois = await RunOnDb(db => db.DomainOfInfluences.Where(doi => doi.CantonDefaults.Canton == DomainOfInfluenceCanton.Sg).ToListAsync());
        affectedDois.MatchSnapshot("affectedDomainOfInfluences");
    }

    [Fact]
    public Task InvalidSecureConnectTenant()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
                o.SecureConnectId = "123333")),
            StatusCode.InvalidArgument);

    [Fact]
    public Task DuplicateVotingCardChannelShouldThrow()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.EnabledVotingCardChannels[1].VotingChannel = SharedProto.VotingChannel.Paper)),
            StatusCode.InvalidArgument,
            "EnabledVotingCardChannels");

    [Fact]
    public Task EmptyVotingCardChannelShouldThrow()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.EnabledVotingCardChannels.Clear())),
            StatusCode.InvalidArgument,
            "EnabledVotingCardChannels");

    [Fact]
    public Task BallotBoxInvalidVotingCardChannelShouldThrow()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.EnabledVotingCardChannels.Single(x => x.VotingChannel == SharedProto.VotingChannel.BallotBox).Valid = false)),
            StatusCode.InvalidArgument,
            "EnabledVotingCardChannels");

    [Fact]
    public Task NotFound()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.Id = "40448f0e-0151-44f3-8188-9cd7613ba503")),
            StatusCode.NotFound);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CantonSettingsService.CantonSettingsServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }

    private UpdateCantonSettingsRequest NewValidRequest(
        Action<UpdateCantonSettingsRequest>? customizer = null)
    {
        var request = new UpdateCantonSettingsRequest
        {
            Id = CantonSettingsMockedData.IdStGallen,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            AuthorityName = "St.Gallen",
            ProportionalElectionMandateAlgorithms =
                {
                    SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum,
                    SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
                },
            MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
            MajorityElectionInvalidVotes = false,
            SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.SeparateCountingCircle,
            SwissAbroadVotingRightDomainOfInfluenceTypes =
                {
                    SharedProto.DomainOfInfluenceType.Ch,
                    SharedProto.DomainOfInfluenceType.Ki,
                },
            EnabledVotingCardChannels =
                {
                    new ServiceModels.CantonSettingsVotingCardChannel
                    {
                        Valid = true,
                        VotingChannel = SharedProto.VotingChannel.Paper,
                    },
                    new ServiceModels.CantonSettingsVotingCardChannel
                    {
                        Valid = true,
                        VotingChannel = SharedProto.VotingChannel.BallotBox,
                    },
                    new ServiceModels.CantonSettingsVotingCardChannel
                    {
                        Valid = true,
                        VotingChannel = SharedProto.VotingChannel.ByMail,
                    },
                },
            VotingDocumentsEVotingEaiMessageType = "1234567",
            ProtocolDomainOfInfluenceSortType = SharedProto.ProtocolDomainOfInfluenceSortType.Alphabetical,
            ProtocolCountingCircleSortType = SharedProto.ProtocolCountingCircleSortType.SortNumber,
        };
        customizer?.Invoke(request);
        return request;
    }
}
