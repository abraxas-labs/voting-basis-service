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
        await RunScoped<EndContestTestingPhaseJob>(job => job.Run(CancellationToken.None));

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
}
