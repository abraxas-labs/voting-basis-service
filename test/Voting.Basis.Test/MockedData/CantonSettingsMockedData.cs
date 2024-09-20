// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Data.Utils;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Basis.Test.MockedData;

public static class CantonSettingsMockedData
{
    public static readonly Guid GuidStGallen = BasisUuidV5.BuildCantonSettings(DomainOfInfluenceCanton.Sg);
    public static readonly Guid GuidZurich = BasisUuidV5.BuildCantonSettings(DomainOfInfluenceCanton.Zh);

    public static readonly string IdStGallen = GuidStGallen.ToString();
    public static readonly string IdZurich = GuidZurich.ToString();

    public static CantonSettings StGallen
        => new CantonSettings
        {
            Id = GuidStGallen,
            Canton = DomainOfInfluenceCanton.Sg,
            AuthorityName = "St.Gallen",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            ProportionalElectionMandateAlgorithms = new List<ProportionalElectionMandateAlgorithm>
            {
                    ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            },
            MajorityElectionAbsoluteMajorityAlgorithm = CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
            MajorityElectionInvalidVotes = false,
            SwissAbroadVotingRight = SwissAbroadVotingRight.SeparateCountingCircle,
            SwissAbroadVotingRightDomainOfInfluenceTypes = new List<DomainOfInfluenceType>
            {
                    DomainOfInfluenceType.Ch,
            },
            EnabledPoliticalBusinessUnionTypes =
            {
                    PoliticalBusinessUnionType.ProportionalElection,
            },
            EnabledVotingCardChannels =
            {
                    new CantonSettingsVotingCardChannel
                    {
                        Id = Guid.Parse("b4755913-4979-4f50-8249-eab36a8ff359"),
                        Valid = true,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new CantonSettingsVotingCardChannel
                    {
                        Id = Guid.Parse("46f39154-010f-4d1f-abb3-f1c18f00e162"),
                        Valid = false,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new CantonSettingsVotingCardChannel
                    {
                        Id = Guid.Parse("cd03d971-c9f3-49e0-8968-e070e8733d6d"),
                        Valid = true,
                        VotingChannel = VotingChannel.BallotBox,
                    },
                    new CantonSettingsVotingCardChannel
                    {
                        Id = Guid.Parse("b9469532-758f-4383-a143-5e6cbbbc51e3"),
                        Valid = true,
                        VotingChannel = VotingChannel.Paper,
                    },
            },
            VotingDocumentsEVotingEaiMessageType = "1234567",
            ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.SortNumber,
            ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.SortNumber,
            MultipleVoteBallotsEnabled = false,
            ProportionalElectionUseCandidateCheckDigit = false,
            MajorityElectionUseCandidateCheckDigit = false,
            PublishResultsEnabled = true,
        };

    public static CantonSettings Zurich
        => new CantonSettings
        {
            Id = GuidZurich,
            Canton = DomainOfInfluenceCanton.Zh,
            AuthorityName = "Zürich",
            SecureConnectId = "zürich-sec-id",
            ProportionalElectionMandateAlgorithms = new List<ProportionalElectionMandateAlgorithm>
            {
                    ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum,
                    ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            },
            MajorityElectionAbsoluteMajorityAlgorithm = CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
            MajorityElectionInvalidVotes = true,
            SwissAbroadVotingRight = SwissAbroadVotingRight.OnEveryCountingCircle,
            SwissAbroadVotingRightDomainOfInfluenceTypes = new List<DomainOfInfluenceType>
            {
                    DomainOfInfluenceType.Ch,
                    DomainOfInfluenceType.Ct,
                    DomainOfInfluenceType.Bz,
            },
            EnabledPoliticalBusinessUnionTypes =
            {
                    PoliticalBusinessUnionType.ProportionalElection,
                    PoliticalBusinessUnionType.MajorityElection,
            },
            EnabledVotingCardChannels =
            {
                    new CantonSettingsVotingCardChannel
                    {
                        Id = Guid.Parse("b52ae482-82f1-4d5a-974d-6775fcd7cbc6"),
                        Valid = true,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new CantonSettingsVotingCardChannel
                    {
                        Id = Guid.Parse("3f3c364e-f4bd-4889-bbcb-283c46912b43"),
                        Valid = true,
                        VotingChannel = VotingChannel.BallotBox,
                    },
                    new CantonSettingsVotingCardChannel
                    {
                        Id = Guid.Parse("804d2f51-0082-49bc-bacc-3ba014e32bc5"),
                        Valid = true,
                        VotingChannel = VotingChannel.Paper,
                    },
            },
            VotingDocumentsEVotingEaiMessageType = "1234567",
            ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.Alphabetical,
            ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.Alphabetical,
            MultipleVoteBallotsEnabled = true,
            CountingMachineEnabled = true,
            NewZhFeaturesEnabled = true,
            ProportionalElectionUseCandidateCheckDigit = true,
            MajorityElectionUseCandidateCheckDigit = true,
            CountingCircleResultStateDescriptions =
            {
                new CountingCircleResultStateDescription
                {
                    Id = Guid.Parse("01e6a947-c04d-436b-82c3-bd53290311c9"),
                    State = CountingCircleResultState.AuditedTentatively,
                    Description = "geprüft",
                },
            },
            StatePlausibilisedDisabled = true,
            EndResultFinalizeDisabled = true,
            CreateContestOnHighestHierarchicalLevelEnabled = true,
            InternalPlausibilisationDisabled = true,
            PublishResultsBeforeAuditedTentatively = true,
        };

    public static IEnumerable<CantonSettings> All
    {
        get
        {
            yield return StGallen;
            yield return Zurich;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var cantonSettingsRepo = sp.GetRequiredService<CantonSettingsRepo>();
            var doiCantonDefaultsBuilder = sp.GetRequiredService<DomainOfInfluenceCantonDefaultsBuilder>();

            foreach (var cantonSettings in All)
            {
                await cantonSettingsRepo.Create(cantonSettings);
                await doiCantonDefaultsBuilder.RebuildForCanton(cantonSettings);
            }

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var cantonSettingsAggregates = All.Select(cantonSettings => ToAggregate(cantonSettings, aggregateFactory, mapper));
            foreach (var cantonSettingsAggregate in cantonSettingsAggregates)
            {
                await aggregateRepository.Save(cantonSettingsAggregate);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });
    }

    private static CantonSettingsAggregate ToAggregate(
        CantonSettings cantonSettings,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<CantonSettingsAggregate>();
        var data = mapper.Map<Core.Domain.CantonSettings>(cantonSettings);
        aggregate.CreateFrom(data);
        return aggregate;
    }
}
