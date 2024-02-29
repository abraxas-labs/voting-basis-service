// (c) Copyright 2024 by Abraxas Informatik AG
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

public class CantonSettingsCreateTest : BaseGrpcTest<CantonSettingsService.CantonSettingsServiceClient>
{
    public CantonSettingsCreateTest(TestApplicationFactory factory)
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
        var response = await AdminClient.CreateAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CantonSettingsCreated>();

        eventData.CantonSettings.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", c => c.CantonSettings.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        await RunOnDb(db =>
          {
              db.DomainOfInfluences.Add(new DomainOfInfluence
              {
                  Id = Guid.Parse("e84a3f1e-c2ea-422c-904e-130b822aad64"),
                  Canton = DomainOfInfluenceCanton.Tg,
                  Type = DomainOfInfluenceType.Ch,
              });
              return db.SaveChangesAsync();
          });

        await TestEventPublisher.Publish(
            new CantonSettingsCreated
            {
                CantonSettings = new CantonSettingsEventData
                {
                    Id = "2d203a3c-40ba-4b53-a57e-38909d71390c",
                    Canton = SharedProto.DomainOfInfluenceCanton.Tg,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                    AuthorityName = "Staatskanzlei Thurgau",
                    ProportionalElectionMandateAlgorithms =
                    {
                            SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    },
                    MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates,
                    MajorityElectionInvalidVotes = true,
                    SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.OnEveryCountingCircle,
                    SwissAbroadVotingRightDomainOfInfluenceTypes =
                    {
                            SharedProto.DomainOfInfluenceType.Ch,
                            SharedProto.DomainOfInfluenceType.An,
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
                    ProtocolCountingCircleSortType = SharedProto.ProtocolCountingCircleSortType.Alphabetical,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var cantonSettingsList = await AdminClient.ListAsync(new ListCantonSettingsRequest());
        cantonSettingsList.CantonSettingsList_.Should().HaveCount(3);
        cantonSettingsList.MatchSnapshot("cantonSettingsList");

        var affectedDois = await RunOnDb(db => db.DomainOfInfluences.Where(doi => doi.CantonDefaults.Canton == DomainOfInfluenceCanton.Tg).ToListAsync());
        affectedDois.MatchSnapshot("affectedDomainOfInfluences");
    }

    [Theory]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum)]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)]
    public async Task TestProcessorWithDeprecatedProportionalElectionMandateAlgorithms(
        SharedProto.ProportionalElectionMandateAlgorithm deprecatedMandateAlgorithm,
        SharedProto.ProportionalElectionMandateAlgorithm expectedMandateAlgorithm)
    {
        var id = "2d203a3c-40ba-4b53-a57e-38909d71390c";

        await TestEventPublisher.Publish(
            new CantonSettingsCreated
            {
                CantonSettings = new CantonSettingsEventData
                {
                    Id = id,
                    Canton = SharedProto.DomainOfInfluenceCanton.Tg,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                    AuthorityName = "Staatskanzlei Thurgau",
                    ProportionalElectionMandateAlgorithms =
                    {
                            SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                            deprecatedMandateAlgorithm,
                    },
                    MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates,
                    SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.OnEveryCountingCircle,
                    VotingDocumentsEVotingEaiMessageType = "1234567",
                    ProtocolDomainOfInfluenceSortType = SharedProto.ProtocolDomainOfInfluenceSortType.Alphabetical,
                    ProtocolCountingCircleSortType = SharedProto.ProtocolCountingCircleSortType.Alphabetical,
                },
                EventInfo = GetMockedEventInfo(),
            });

        var cantonSettings = await AdminClient.GetAsync(new() { Id = id });
        cantonSettings.ProportionalElectionMandateAlgorithms.Should().Contain(expectedMandateAlgorithm);
    }

    [Fact]
    public Task InvalidSecureConnectTenant()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
                o.SecureConnectId = "123333")),
            StatusCode.InvalidArgument);

    [Fact]
    public Task DuplicateVotingCardChannelShouldThrow()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.EnabledVotingCardChannels[1].VotingChannel = SharedProto.VotingChannel.Paper)),
            StatusCode.InvalidArgument,
            "EnabledVotingCardChannels");

    [Fact]
    public Task EmptyVotingCardChannelShouldThrow()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.EnabledVotingCardChannels.Clear())),
            StatusCode.InvalidArgument,
            "EnabledVotingCardChannels");

    [Fact]
    public Task BallotBoxInvalidVotingCardChannelShouldThrow()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.EnabledVotingCardChannels.Single(x => x.VotingChannel == SharedProto.VotingChannel.BallotBox).Valid = false)),
            StatusCode.InvalidArgument,
            "EnabledVotingCardChannels");

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CantonSettingsService.CantonSettingsServiceClient(channel)
            .CreateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }

    private CreateCantonSettingsRequest NewValidRequest(
        Action<CreateCantonSettingsRequest>? customizer = null)
    {
        var request = new CreateCantonSettingsRequest
        {
            Canton = SharedProto.DomainOfInfluenceCanton.Tg,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            AuthorityName = "Thurgau",
            ElectoralRegistrationEnabled = true,
            CountingMachineEnabled = true,
            ProportionalElectionMandateAlgorithms =
                {
                    SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                },
            MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
            MajorityElectionInvalidVotes = true,
            SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.OnEveryCountingCircle,
            SwissAbroadVotingRightDomainOfInfluenceTypes =
                {
                    SharedProto.DomainOfInfluenceType.Ch,
                },
            EnabledPoliticalBusinessUnionTypes =
                {
                    SharedProto.PoliticalBusinessUnionType.PoliticalBusinessUnionProportionalElection,
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
            ProtocolCountingCircleSortType = SharedProto.ProtocolCountingCircleSortType.Alphabetical,
            MultipleVoteBallotsEnabled = true,
            ProportionalElectionUseCandidateCheckDigit = true,
            MajorityElectionUseCandidateCheckDigit = true,
        };
        customizer?.Invoke(request);
        return request;
    }
}
