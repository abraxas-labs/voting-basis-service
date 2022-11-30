// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using eCH_0155_4_0;
using Voting.Basis.Ech.Extensions;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class ElectionMapping
{
    internal static ElectionDescriptionInformationType ToEchElectionDescription(this DataModels.PoliticalBusiness election)
    {
        var descriptionInfos = new List<ElectionDescriptionInfoType>();

        foreach (var (lang, officialDescription) in election.OfficialDescription)
        {
            election.ShortDescription.TryGetValue(lang, out var shortDescription);

            // Truncating to 255, since eCH doesn't allow any longer strings in this field.
            descriptionInfos.Add(ElectionDescriptionInfoType.Create(lang, officialDescription.Truncate(255), shortDescription));
        }

        return ElectionDescriptionInformationType.Create(descriptionInfos);
    }
}
