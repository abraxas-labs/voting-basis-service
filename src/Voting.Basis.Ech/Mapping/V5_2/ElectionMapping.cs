// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Ech0155_5_2;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping.V5_2;

internal static class ElectionMapping
{
    internal static ElectionDescriptionInformationType ToEchElectionDescription(this DataModels.PoliticalBusiness election, bool eVoting)
    {
        var descriptionInfos = new ElectionDescriptionInformationType();

        var officialDescriptionTranslations = election.OfficialDescription.FilterEchExportLanguages(eVoting);
        var shortDescriptionTranslations = election.ShortDescription.FilterEchExportLanguages(eVoting);

        foreach (var (lang, officialDescription) in officialDescriptionTranslations)
        {
            shortDescriptionTranslations.TryGetValue(lang, out var shortDescription);

            // Truncating to 255, since eCH doesn't allow any longer strings in this field.
            descriptionInfos.ElectionDescriptionInfo.Add(new ElectionDescriptionInformationTypeElectionDescriptionInfo
            {
                Language = lang,
                ElectionDescription = officialDescription.Truncate(255),
                ElectionDescriptionShort = shortDescription,
            });
        }

        return descriptionInfos;
    }
}
