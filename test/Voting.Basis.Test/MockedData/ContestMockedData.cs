// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;

namespace Voting.Basis.Test.MockedData;

public static class ContestMockedData
{
    public const string IdArchivedContest = "2504a611-08a7-49ed-bab8-666b3add94b9";
    public const string IdPastLockedContest = "4b1513ce-89fd-4ce8-bf71-7b55a50b8943";
    public const string IdPastLockedContestNoPoliticalBusinesses = "4c3adb88-2de9-4042-bf38-3fda1cb8b29a";
    public const string IdPastUnlockedContest = "16528771-d8b7-46f6-b7ed-e16c36ea3197";
    public const string IdBundContest = "20361fdf-7d18-47b9-bdef-fd82efe8a6a7";
    public const string IdStGallenEvoting = "95825eb0-0f52-461a-a5f8-23fb35fa69e1";
    public const string IdGossau = "bfd88d88-77f2-4172-a73d-56b1ca5442b3";
    public const string IdUzwilEvoting = "cc70fe43-8f4e-4bc6-a461-b808907bc996";
    public const string IdKirche = "a091a5cc-735b-4bf4-a30d-f4c907c9fc10";
    public const string IdThurgauNoPoliticalBusinesses = "dc0f4940-10c2-4a33-b752-3e5d761c0009";

    public static Contest ArchivedContest
        => new Contest
        {
            Id = Guid.Parse(IdArchivedContest),
            Date = new DateTime(2018, 11, 24, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Nationalratswahlen in der Verangenheit (archiviert)"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            EndOfTestingPhase = new DateTime(2018, 11, 20, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.Archived,
            EVoting = true,
            EVotingFrom = new DateTime(2018, 10, 24, 10, 0, 0, DateTimeKind.Utc),
            EVotingTo = new DateTime(2018, 10, 28, 10, 0, 0, DateTimeKind.Utc),
        };

    public static Contest PastLockedContest
        => new Contest
        {
            Id = Guid.Parse(IdPastLockedContest),
            Date = new DateTime(2019, 11, 24, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Nationalratswahlen in der Verangenheit"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            EndOfTestingPhase = new DateTime(2019, 11, 23, 10, 0, 0, DateTimeKind.Utc),
            State = ContestState.PastLocked,
            EVoting = true,
            EVotingFrom = new DateTime(2019, 10, 24, 10, 0, 0, DateTimeKind.Utc),
            EVotingTo = new DateTime(2019, 10, 28, 10, 0, 0, DateTimeKind.Utc),
        };

    public static Contest PastLockedContestNoPoliticalBusinesses
        => new Contest
        {
            Id = Guid.Parse(IdPastLockedContestNoPoliticalBusinesses),
            Date = new DateTime(2018, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Urnengang der Vergangenheit"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            EndOfTestingPhase = new DateTime(2018, 10, 1, 16, 0, 0, DateTimeKind.Utc),
            State = ContestState.PastLocked,
        };

    public static Contest PastUnlockedContest
        => new Contest
        {
            Id = Guid.Parse(IdPastUnlockedContest),
            Date = new DateTime(2019, 11, 24, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Entsperrte Nationalratswahlen in der Verangenheit"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            EndOfTestingPhase = new DateTime(2019, 11, 23, 10, 0, 0, DateTimeKind.Utc),
            State = ContestState.PastUnlocked,
            EVoting = true,
            EVotingFrom = new DateTime(2019, 10, 24, 10, 0, 0, DateTimeKind.Utc),
            EVotingTo = new DateTime(2019, 10, 28, 10, 0, 0, DateTimeKind.Utc),
        };

    public static Contest BundContest
        => new Contest
        {
            Id = Guid.Parse(IdBundContest),
            Date = new DateTime(2029, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Nationalratswahlen in der Zukunft"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidBund,
            EndOfTestingPhase = new DateTime(2029, 2, 9, 12, 1, 0, DateTimeKind.Utc),
            PreviousContestId = Guid.Parse(IdPastLockedContest),
        };

    public static Contest StGallenEvotingContest
        => new Contest
        {
            Id = Guid.Parse(IdStGallenEvoting),
            Date = new DateTime(2020, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Kantonsurnengang"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidStGallen,
            EndOfTestingPhase = new DateTime(2020, 8, 28, 3, 59, 0, DateTimeKind.Utc),
            EVoting = true,
            EVotingFrom = new DateTime(2020, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            EVotingTo = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        };

    public static Contest GossauContest
        => new Contest
        {
            Id = Guid.Parse(IdGossau),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Gossau Urnengang"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            EndOfTestingPhase = new DateTime(2020, 2, 27, 16, 30, 0, DateTimeKind.Utc),
        };

    public static Contest UzwilEvotingContest
        => new Contest
        {
            Id = Guid.Parse(IdUzwilEvoting),
            Date = new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Urnengang in Uzwil"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidUzwil,
            EndOfTestingPhase = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EVoting = true,
            EVotingFrom = new DateTime(2020, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            EVotingTo = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        };

    public static Contest KirchenContest
        => new Contest
        {
            Id = Guid.Parse(IdKirche),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Kirchen-Urnengang"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidKirchgemeinde,
            EndOfTestingPhase = new DateTime(2020, 2, 27, 0, 0, 0, DateTimeKind.Utc),
        };

    public static Contest ThurgauNoPoliticalBusinessesContest
        => new Contest
        {
            Id = Guid.Parse(IdThurgauNoPoliticalBusinesses),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Description = LanguageUtil.MockAllLanguages("Thurgau-Urnengang"),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidThurgau,
            EndOfTestingPhase = new DateTime(2020, 2, 28, 0, 0, 0, DateTimeKind.Utc),
        };

    public static IEnumerable<Contest> All
    {
        get
        {
            yield return ArchivedContest;
            yield return PastLockedContest;
            yield return PastLockedContestNoPoliticalBusinesses;
            yield return PastUnlockedContest;
            yield return BundContest;
            yield return StGallenEvotingContest;
            yield return GossauContest;
            yield return UzwilEvotingContest;
            yield return KirchenContest;
            yield return ThurgauNoPoliticalBusinessesContest;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await DomainOfInfluenceMockedData.Seed(runScoped);
        await PreconfiguredContestDateMockedData.Seed(runScoped);

        var all = All.ToList();

        foreach (var contest in all)
        {
            contest.PastLockPer = contest.Date.NextUtcDate(true);
        }

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var contestCache = sp.GetRequiredService<ContestCache>();
            var asymmetricAlgorithmAdapter = sp.GetRequiredService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();

            contestCache.Clear();

            db.Contests.AddRange(all);
            await db.SaveChangesAsync();

            foreach (var contest in all)
            {
                // contests have per default an unlimited valid key for performance reasons.
                contestCache.Add(new()
                {
                    Id = contest.Id,
                    KeyData = new ContestCacheEntryKeyData(asymmetricAlgorithmAdapter.CreateRandomPrivateKey(), DateTime.MinValue, DateTime.MaxValue),
                });
            }

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var contestAggregates = All.Select(c => ToAggregate(c, aggregateFactory, mapper, MockedClock.UtcNowDate));

            foreach (var contest in contestAggregates)
            {
                await aggregateRepository.Save(contest);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });
    }

    public static ContestAggregate ToAggregate(Contest contest, IAggregateFactory aggregateFactory, TestMapper mapper, DateTime now)
    {
        var aggregate = aggregateFactory.New<ContestAggregate>();
        var c = mapper.Map<Core.Domain.Contest>(contest);

        // since only testing phase > now is allowed,
        // we need to set the system time for the create method accordingly.
        if (c.EndOfTestingPhase < MockedClock.UtcNowDate)
        {
            AdjustableMockedClock.OverrideUtcNow = c.EndOfTestingPhase;
        }

        aggregate.CreateFrom(c);
        AdjustableMockedClock.OverrideUtcNow = null;

        if (c.EndOfTestingPhase <= now)
        {
            aggregate.TryEndTestingPhase();
        }

        switch (contest.State)
        {
            case ContestState.PastLocked:
                aggregate.TrySetPastLocked();
                break;
            case ContestState.PastUnlocked:
                aggregate.TrySetPastLocked();
                aggregate.PastUnlock();
                break;
            case ContestState.Archived:
                aggregate.TrySetPastLocked();
                aggregate.TryArchive();
                break;
        }

        return aggregate;
    }
}
