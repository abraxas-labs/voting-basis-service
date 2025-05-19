// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0010_6_0;
using Ech0155_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Resources;
using Voting.Basis.Ech.Utils;
using Voting.Lib.Common;
using Voting.Lib.Ech.Ech0157_4_0.Models;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class CandidateMapping
{
    private static readonly DateTime DefaultDateOfBirth = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static CandidateType ToEchCandidateType(this DataModels.ElectionCandidate candidate, Dictionary<string, string>? party, DataModels.DomainOfInfluenceCanton canton, PoliticalBusinessType politicalBusinessType)
    {
        var text = candidate.ToEchCandidateText(canton, politicalBusinessType, party);

        var occupationInfos = candidate.Occupation
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => new OccupationalTitleInformationTypeOccupationalTitleInfo
            {
                Language = x.Key,
                OccupationalTitle = x.Value,
            })
            .ToList();

        var partyInfos = party?
            .Select(x => new PartyAffiliationformationTypePartyAffiliationInfo
            {
                Language = x.Key,
                PartyAffiliationShort = x.Value,
            })
            .ToList();

        var zipCodeIsSwiss = int.TryParse(candidate.ZipCode, out var zipCode) && zipCode is > 1000 and <= 9999;
        var country = CountryUtils.GetCountryFromIsoId(candidate.Country);

        return new CandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            FamilyName = candidate.PoliticalLastName,
            FirstName = candidate.FirstName,
            CallName = candidate.PoliticalFirstName,
            Title = candidate.Title,
            CandidateReference = candidate.Number,
            CandidateText = text.CandidateTextInfo,
            DateOfBirth = candidate.DateOfBirth ?? DefaultDateOfBirth,
            Sex = candidate.Sex.ToEchSexType(),
            OccupationalTitle = occupationInfos.Count == 0 ? null : occupationInfos,
            DwellingAddress = new AddressInformationType
            {
                SwissZipCode = zipCodeIsSwiss ? (uint?)zipCode : null,
                ForeignZipCode = zipCodeIsSwiss ? null : candidate.ZipCode,
                Town = candidate.Locality != string.Empty ? candidate.Locality : UnknownMapping.UnknownValue,
                Street = candidate.Street,
                HouseNumber = candidate.HouseNumber,
                Country = new CountryType
                {
                    CountryId = (ushort)(country?.Id ?? CountryUtils.SwissCountryId),
                    CountryIdIso2 = country?.IsoId ?? CountryUtils.SwissCountryIso,
                    CountryNameShort = country?.Description ?? CountryUtils.SwissCountryNameShort,
                },
            },
            Swiss = new List<string> { candidate.Origin != string.Empty ? candidate.Origin : UnknownMapping.UnknownValue },
            MrMrs = candidate.Sex.ToEchMrMrsType(),
            LanguageOfCorrespondence = Languages.German,
            PartyAffiliation = partyInfos?.Count == 0 ? null : partyInfos,
            Role = null,
        };
    }

    internal static T ToBasisCandidate<T>(this CandidateType candidate, IdLookup idLookup, ElectionInformationExtensionCandidate? candidateExtension)
        where T : DataModels.ElectionCandidate, new()
    {
        var occupation = candidate
            .OccupationalTitle
            ?.ToLanguageDictionary(x => x.Language, x => x.OccupationalTitle, string.Empty, true)
            ?? new Dictionary<string, string>();

        Dictionary<string, string> titleAndOccupation = new();
        if (!string.IsNullOrEmpty(candidateExtension?.TitleAndOccupation))
        {
            titleAndOccupation.Add(Languages.German, candidateExtension.TitleAndOccupation);
            LanguageMapping.FillAllLanguages(titleAndOccupation, candidateExtension.TitleAndOccupation);
        }

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
            OccupationTitle = titleAndOccupation,
            Locality = UnknownMapping.MapUnknownValue(candidate.DwellingAddress?.Town),
            ZipCode = candidate.DwellingAddress?.SwissZipCode?.ToString()
                      ?? candidate.DwellingAddress?.ForeignZipCode ?? string.Empty,
            Origin = candidate.SwissSpecified ? UnknownMapping.MapUnknownValue(candidate.Swiss[0]) : string.Empty,
            Street = candidate.DwellingAddress?.Street ?? string.Empty,
            HouseNumber = candidate.DwellingAddress?.HouseNumber ?? string.Empty,
            Country = candidate.DwellingAddress?.Country.CountryIdIso2 ?? CountryUtils.SwissCountryIso,
        };
    }

    internal static CandidateTextInformationType ToEchCandidateText(this DataModels.ElectionCandidate candidate, DataModels.DomainOfInfluenceCanton canton, PoliticalBusinessType politicalBusinessType, Dictionary<string, string>? party = null)
    {
        var dateOfBirthText = DomainOfInfluenceCantonDataTransformer.EchCandidateDateOfBirthText(canton, candidate.DateOfBirth ?? DefaultDateOfBirth);
        var localityText = string.IsNullOrEmpty(candidate.Locality) ? string.Empty : $", {candidate.Locality}";
        var candidateTextBase = $"{dateOfBirthText}{{0}}{localityText}{{1}}{{2}}";
        var textInfos = new CandidateTextInformationType();
        foreach (var language in Languages.All)
        {
            var occupationTitleText = string.Empty;
            if (candidate.OccupationTitle.TryGetValue(language, out var occupationTitle))
            {
                occupationTitleText = $", {occupationTitle}";
            }

            var partyText = string.Empty;
            if (party?.TryGetValue(language, out string? partyTranslatedText) != null)
            {
                partyTranslatedText = DomainOfInfluenceCantonDataTransformer.EchCandidatePartyText(canton, politicalBusinessType, partyTranslatedText);
                partyText = !string.IsNullOrEmpty(partyTranslatedText) ? $", {partyTranslatedText}" : null;
            }

            var incumbentText = string.Empty;
            if (candidate.Incumbent)
            {
                var incumbentTranslatedText = Strings.ResourceManager.GetString($"ElectionCandidate.Incumbent.{language}");
                if (incumbentTranslatedText != null)
                {
                    incumbentText = $", {incumbentTranslatedText}";
                }
            }

            textInfos.CandidateTextInfo.Add(new CandidateTextInformationTypeCandidateTextInfo
            {
                Language = language,
                CandidateText = string.Format(candidateTextBase, occupationTitleText, partyText, incumbentText),
            });
        }

        return textInfos;
    }
}
