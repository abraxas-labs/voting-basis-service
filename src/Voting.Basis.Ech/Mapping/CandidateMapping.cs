// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using eCH_0010_6_0;
using eCH_0155_4_0;
using Voting.Lib.Common;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class CandidateMapping
{
    private const string UnknownValue = "-";
    private const int SwissCountryId = 8100;
    private const string SwissCountryIso = "CH";
    private const string SwissCountryNameShort = "Schweiz";

    internal static CandidateType ToEchCandidateType(this DataModels.ElectionCandidate candidate, Dictionary<string, string>? party)
    {
        var text = candidate.ToEchCandidateText();

        var occupationInfos = candidate.Occupation
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => OccupationalTitleInfo.Create(x.Key, x.Value))
            .ToList();
        var occupation = occupationInfos.Count > 0
            ? OccupationalTitleInformation.Create(occupationInfos)
            : null;

        var partyInfos = party?
            .Select(x => PartyAffiliationInfo.Create(x.Key, x.Value))
            .ToList();

        var zipCodeIsSwiss = int.TryParse(candidate.ZipCode, out var zipCode) && zipCode is > 1000 and <= 9999;
        return CandidateType.Create(
            null,
            candidate.Id.ToString(),
            candidate.LastName,
            candidate.FirstName,
            candidate.PoliticalFirstName,
            candidate.Title,
            candidate.Number,
            null,
            text,
            candidate.DateOfBirth,
            candidate.Sex.ToEchSexType(),
            occupation,
            null,
            null,
            new AddressInformationType
            {
                SwissZipCode = zipCodeIsSwiss ? zipCode : null,
                ForeignZipCode = zipCodeIsSwiss ? null : candidate.ZipCode,
                Town = candidate.Locality != string.Empty ? candidate.Locality : UnknownValue,
                Country = CountryType.Create(SwissCountryId, SwissCountryIso, SwissCountryNameShort),
            },
            Swiss.Create(candidate.Origin != string.Empty ? candidate.Origin : UnknownValue),
            candidate.Sex.ToEchMrMrsType(),
            Languages.German,
            candidate.Incumbent,
            null,
            partyInfos?.Count > 0 ? PartyAffiliationInformation.Create(partyInfos) : null);
    }

    internal static T ToBasisCandidate<T>(this CandidateType candidate, IdLookup idLookup)
        where T : DataModels.ElectionCandidate, new()
    {
        var occupation = candidate
            .OccupationalTitle
            ?.OccupationalTitleInfo
            ?.ToDictionary(x => x.Language, x => x.OccupationalTitle)
            ?? new Dictionary<string, string>();

        return new T
        {
            Id = idLookup.GuidForId(candidate.CandidateIdentification),
            LastName = candidate.FamilyName,
            FirstName = candidate.FirstName,
            PoliticalFirstName = candidate.CallName,
            PoliticalLastName = candidate.FamilyName,
            Title = candidate.Title ?? string.Empty,
            Number = candidate.CandidateReference.Split('.').Last(), // May contain the list number, e.g. 02.05
            DateOfBirth = DateTime.SpecifyKind(candidate.DateOfBirth, DateTimeKind.Utc),
            Incumbent = candidate.IncumbentYesNo ?? false,
            Sex = candidate.Sex.ToBasisSexType(),
            Occupation = occupation,
            Locality = MapUnknownValue(candidate.DwellingAddress?.Town),
            ZipCode = candidate.DwellingAddress?.SwissZipCode?.ToString()
                      ?? candidate.DwellingAddress?.ForeignZipCode ?? string.Empty,
            Origin = candidate.SwissForeignChoice is Swiss swiss ? MapUnknownValue(swiss.Origin) : string.Empty,
        };
    }

    internal static CandidateTextInformation ToEchCandidateText(this DataModels.ElectionCandidate candidate)
    {
        var candidateTextBase = $"{candidate.DateOfBirth:dd.MM.yyyy}, {{0}}{candidate.Locality}";
        var textInfos = new List<CandidateTextInfo>();
        foreach (var language in Languages.All)
        {
            var occupationTitleText = string.Empty;
            if (candidate.OccupationTitle.TryGetValue(language, out var occupationTitle))
            {
                occupationTitleText = $"{occupationTitle}, ";
            }

            textInfos.Add(CandidateTextInfo.Create(language, string.Format(candidateTextBase, occupationTitleText)));
        }

        return CandidateTextInformation.Create(textInfos);
    }

    private static string MapUnknownValue(string? value)
    {
        return value is null or UnknownValue ? string.Empty : value;
    }
}
