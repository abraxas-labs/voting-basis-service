// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Ech0155_5_1;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping.V5;

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
            ? new EVotingPeriodType { EVotingPeriodFrom = contest.EVotingFrom!.Value, EVotingPeriodTill = contest.EVotingTo!.Value }
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

    internal static Contest ToBasisContest(this ContestType contest, IdLookup idLookup)
    {
        var descriptionInfos = contest.ContestDescription;
        var description = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ContestDescription, contest.ContestIdentification);

        var contestDate = contest.ContestDate.MapToUtcDateTime();
        return new Contest
        {
            Id = idLookup.GuidForId(contest.ContestIdentification),
            Date = contestDate,
            EVoting = contest.EVotingPeriod != null,
            EVotingFrom = contest.EVotingPeriod?.EVotingPeriodFrom.MapToUtcDateTime(),
            EVotingTo = contest.EVotingPeriod?.EVotingPeriodTill.MapToUtcDateTime(),
            Description = description,
            EndOfTestingPhase = DateTime.UtcNow, // required, otherwise we have error because the DateTime is not in UTC
        };
    }
}
