// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Permission;
using Voting.Lib.Eventing.Domain;

namespace Voting.Basis.Core.Seeding;

public class PreconfiguredContestDateSeedSource : BaseSeedSource<PreconfiguredContestDateAggregate>
{
    private readonly ILogger<PreconfiguredContestDateSeedSource> _logger;

    public PreconfiguredContestDateSeedSource(
        PermissionService permissionService,
        IAggregateFactory aggregateFactory,
        ILogger<PreconfiguredContestDateSeedSource> logger)
        : base("preconfigured_contest_dates.json", permissionService, aggregateFactory)
    {
        _logger = logger;
    }

    protected override void FillAggregate(PreconfiguredContestDateAggregate aggregate, string rawSeedData)
    {
        var dates = JsonSerializer.Deserialize<List<DateTime>>(rawSeedData);

        if (dates == null)
        {
            _logger.LogWarning($"{nameof(PreconfiguredContestDateSeedSource)}-{nameof(FillAggregate)}: Unable to fill aggregate since deserialization of raw seed data to a list of dates returned null");
            return;
        }

        foreach (var date in dates)
        {
            aggregate.AddDate(date);
        }
    }
}
