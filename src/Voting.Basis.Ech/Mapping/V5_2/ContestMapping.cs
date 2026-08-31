// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Ech0155_5_2;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping.V5_2;

internal static class ContestMapping
{
    internal static ContestType ToEchContestType(this Contest contest)
    {
        var contestDescriptionInfos = contest.Description
            .FilterEchExportLanguages(contest.EVoting)
            .Select(x => new ContestDescriptionInformationTypeContestDescriptionInfo
            {
                Language = x.Key,
                ContestDescription = x.Value,
            })
            .ToList();

        var eVotingPeriod = contest.EVoting
            ? new EVotingPeriodType
            {
                EVotingPeriodFrom = contest.EVotingFrom!.Value.ConvertUtcTimeToSwissTime(),
                EVotingPeriodTill = contest.EVotingTo!.Value.ConvertUtcTimeToSwissTime(),
            }
            : null;

        var contestType = new ContestType
        {
            ContestIdentification = contest.Id.ToString(),
            ContestDate = contest.Date,
            ContestDescription = contestDescriptionInfos,
            EVotingPeriod = eVotingPeriod,
        };

        return contestType;
    }
}
