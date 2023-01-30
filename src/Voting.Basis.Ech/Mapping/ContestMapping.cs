// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using eCH_0155_4_0;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class ContestMapping
{
    internal static ContestType ToEchContestType(this Contest contest)
    {
        var contestDescriptionInfos = contest.Description
            .Select(x => ContestDescriptionInfo.Create(x.Key, x.Value))
            .ToList();
        var contestDescription = ContestDescriptionInformation.Create(contestDescriptionInfos);

        var eVotingPeriod = contest.EVoting
            ? EvotingPeriodType.Create(contest.EVotingFrom!.Value, contest.EVotingTo!.Value)
            : null;

        return ContestType.Create(contest.Id.ToString(), contest.Date, contestDescription, eVotingPeriod);
    }

    internal static Contest ToBasisContest(this ContestType contest, IdLookup idLookup)
    {
        var descriptionInfos = contest
            .ContestDescription
            ?.ContestDescriptionInfo;
        var description = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ContestDescription, contest.ContestIdentification);

        var contestDate = contest.ContestDate.MapToUtcDateTime();
        return new Contest
        {
            Id = idLookup.GuidForId(contest.ContestIdentification),
            Date = contestDate,
            EVoting = contest.EvotingPeriod != null,
            EVotingFrom = contest.EvotingPeriod?.EvotingPeriodFrom.MapToUtcDateTime(),
            EVotingTo = contest.EvotingPeriod?.EvotingPeriodTill.MapToUtcDateTime(),
            Description = description,
            EndOfTestingPhase = DateTime.UtcNow, // required, otherwise we have error because the DateTime is not in UTC
        };
    }
}
