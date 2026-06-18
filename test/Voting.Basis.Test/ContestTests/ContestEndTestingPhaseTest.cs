// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Data.Models;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Utils;
using Xunit;
using Contest = Voting.Basis.Core.Domain.Contest;

namespace Voting.Basis.Test.ContestTests;

public class ContestEndTestingPhaseTest : BaseTest
{
    private static readonly Guid ContestId = Guid.Parse("1bda1ff7-8ffa-4bda-a440-1af6ca5f6bc7");

    public ContestEndTestingPhaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        await RunScoped<IServiceProvider>(async sp =>
        {
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());
            var createContestEvent = NewValidCreationEvent();
            var contestId = Guid.Parse(createContestEvent.Contest.Id);
            await TestEventPublisher.Publish(createContestEvent);

            var contestAggregate = sp.GetRequiredService<IAggregateFactory>().New<ContestAggregate>();
            AdjustableMockedClock.OverrideUtcNow = createContestEvent.Contest.EndOfTestingPhase.ToDateTime();
            var domainContest = RunScoped<TestMapper, Contest>(mapper => mapper.Map<Contest>(createContestEvent.Contest));
            contestAggregate.CreateFrom(domainContest);
            AdjustableMockedClock.OverrideUtcNow = null;

            var contestCache = sp.GetRequiredService<ContestCache>();
            var asymmetricAlgorithmAdapter = sp.GetRequiredService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();

            if (contestCache.Get(contestId) != null)
            {
                contestCache.Remove(contestId);
            }

            contestCache.Add(new()
            {
                Id = contestId,
                KeyData = new ContestCacheEntryKeyData(asymmetricAlgorithmAdapter.CreateRandomPrivateKey(), DateTime.MinValue, DateTime.MaxValue),
            });

            await ExecuteOnInfiniteValidContestKey(contestId, sp, () => sp.GetRequiredService<AggregateRepositoryMock>().Save(contestAggregate));
        });
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            1,
            new ContestTestingPhaseEnded
            {
                ContestId = ContestId.ToString(),
            });

        await RunOnDb(async db =>
        {
            var contest = await db.Contests.FindAsync(ContestId);
            contest!.TestingPhaseEnded.Should().BeTrue();
            contest.State.Should().Be(ContestState.Active);
        });
    }

    [Fact]
    public async Task JobShouldEndContestTestingPhase()
    {
        await SeedPoliticalBusinesses();
        await RunScoped<EndContestTestingPhaseJob>(job => job.Run(CancellationToken.None));

        var voteEvents = EventPublisherMock.GetPublishedEvents<VoteTestingPhaseEnded>();
        var peEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionTestingPhaseEnded>();
        var meEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionTestingPhaseEnded>();

        voteEvents.Should().HaveCount(1);
        peEvents.Should().HaveCount(1);
        meEvents.Should().HaveCount(1);

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestTestingPhaseEnded, EventSignatureBusinessMetadata>();
        eventData.ContestId.Should().Be(ContestId.ToString());
        eventMetadata!.ContestId.Should().Be(ContestId.ToString());
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestId.ToString(), async () =>
        {
            await RunScoped<EndContestTestingPhaseJob>(job => job.Run(CancellationToken.None));
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestTestingPhaseEnded>();
        });
    }

    private ContestCreated NewValidCreationEvent()
    {
        return new ContestCreated
        {
            Contest = new ContestEventData
            {
                Id = ContestId.ToString(),
                Date = new DateTime(2019, 1, 3, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2019, 1, 1, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
            },
        };
    }

    private async Task SeedPoliticalBusinesses()
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();

            var domainVote = new Core.Domain.Vote
            {
                Id = Guid.Parse("f5b470d5-acd4-44ce-b7c9-1139c726f075"),
                PoliticalBusinessNumber = "200",
                OfficialDescription = LanguageUtil.MockAllLanguages("VV"),
                ShortDescription = LanguageUtil.MockAllLanguages("VV"),
                InternalDescription = "Abstimmung Bund",
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
                ContestId = ContestId,
                ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
                Active = true,
                BallotBundleSampleSizePercent = 25,
                AutomaticBallotBundleNumberGeneration = true,
                ResultEntry = VoteResultEntry.FinalResults,
                EnforceResultEntryForCountingCircles = true,
                ReviewProcedure = VoteReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                Type = VoteType.QuestionsOnSingleBallot,
            };

            var vote = aggregateFactory.New<VoteAggregate>();
            vote.CreateFrom(domainVote);
            await aggregateRepository.Save(vote);

            var domainProportionalElection = new Core.Domain.ProportionalElection
            {
                Id = Guid.Parse("fb087ed0-6ce2-439e-a329-b292c44e2d8e"),
                PoliticalBusinessNumber = "100",
                OfficialDescription = LanguageUtil.MockAllLanguages("PE"),
                ShortDescription = LanguageUtil.MockAllLanguages("PE"),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
                ContestId = ContestId,
                MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
                NumberOfMandates = 5,
                ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                EnforceCandidateCheckDigitForCountingCircles = true,
            };

            var proportionalElection = aggregateFactory.New<ProportionalElectionAggregate>();
            proportionalElection.CreateFrom(domainProportionalElection);
            await aggregateRepository.Save(proportionalElection);

            var domainMajorityElection = new Core.Domain.MajorityElection
            {
                Id = Guid.Parse("5cf9cf53-cda4-4c46-a9d4-dd0c5748aa1d"),
                PoliticalBusinessNumber = "100",
                OfficialDescription = LanguageUtil.MockAllLanguages("ME"),
                ShortDescription = LanguageUtil.MockAllLanguages("ME"),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
                ContestId = ContestId,
                MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
                NumberOfMandates = 5,
                EnforceReviewProcedureForCountingCircles = true,
                EnforceCandidateCheckDigitForCountingCircles = true,
            };

            var domainSecondaryMajorityElection = new Core.Domain.SecondaryMajorityElection
            {
                Id = Guid.Parse("33ebd95b-0f08-48b3-9494-71ec1190fdc4"),
                PrimaryMajorityElectionId = domainMajorityElection.Id,
                IsOnSeparateBallot = false,
            };

            var domainElectionGroup = new Core.Domain.ElectionGroup
            {
                Id = Guid.Parse("22d3a05f-6cbf-4eb0-9e21-42f8bf8ded4f"),
                PrimaryMajorityElectionId = domainMajorityElection.Id,
                Number = 1,
            };

            var majorityElection = aggregateFactory.New<MajorityElectionAggregate>();
            majorityElection.CreateFrom(domainMajorityElection);
            majorityElection.CreateElectionGroupFrom(domainElectionGroup);
            majorityElection.CreateSecondaryMajorityElectionFrom(domainSecondaryMajorityElection);
            await aggregateRepository.Save(majorityElection);

            var voteEv = EventPublisherMock.GetSinglePublishedEvent<VoteCreated>();
            await TestEventPublisher.Publish(voteEv);

            var peEv = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionCreated>();
            await TestEventPublisher.Publish(peEv);

            var meEv = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCreated>();
            await TestEventPublisher.Publish(meEv);

            var egEv = EventPublisherMock.GetSinglePublishedEvent<ElectionGroupCreated>();
            await TestEventPublisher.Publish(egEv);

            var smeEv = EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionCreated>();
            await TestEventPublisher.Publish(smeEv);
        });
    }
}
