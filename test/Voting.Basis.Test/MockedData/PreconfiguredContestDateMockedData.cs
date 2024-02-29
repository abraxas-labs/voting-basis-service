// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Test.MockedData;

public static class PreconfiguredContestDateMockedData
{
    public static readonly DateTime Date20200209 = new DateTime(2020, 2, 9, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Date20200517 = new DateTime(2020, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Date20200927 = new DateTime(2020, 9, 27, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Date20201129 = new DateTime(2020, 11, 29, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Date20210307 = new DateTime(2021, 3, 7, 0, 0, 0, DateTimeKind.Utc);

    public static PreconfiguredContestDate Preconfigured20200209
        => new PreconfiguredContestDate
        {
            Id = Date20200209,
        };

    public static PreconfiguredContestDate Preconfigured20200517
        => new PreconfiguredContestDate
        {
            Id = Date20200517,
        };

    public static PreconfiguredContestDate Preconfigured20200927
        => new PreconfiguredContestDate
        {
            Id = Date20200927,
        };

    public static PreconfiguredContestDate Preconfigured20201129
        => new PreconfiguredContestDate
        {
            Id = Date20201129,
        };

    public static PreconfiguredContestDate Preconfigured20210307
        => new PreconfiguredContestDate
        {
            Id = Date20210307,
        };

    public static IEnumerable<PreconfiguredContestDate> All
    {
        get
        {
            yield return Preconfigured20200209;
            yield return Preconfigured20200517;
            yield return Preconfigured20200927;
            yield return Preconfigured20201129;
            yield return Preconfigured20210307;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.PreconfiguredContestDates.AddRange(All);
            await db.SaveChangesAsync();
        });
    }
}
