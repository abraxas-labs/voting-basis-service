// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using eCH_0155_4_0;
using eCH_0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech.Ech0157.Models;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class ProportionalElectionMapping
{
    private const string EmptyListId = "99";
    private const string EmptyListOrderNumber = "99";
    private const int EmptyListPosition = 99;
    private const string EmptyListShortDescription = "WoP";
    private static readonly Dictionary<string, string> EmptyListDescriptions = new()
    {
        { Languages.German, "Wahlzettel ohne Parteibezeichnung" },
        { Languages.French, "Bulletin de vote sans désignation de parti" },
        { Languages.Italian, "Scheda elettorale senza indicazione del partito" },
        { Languages.Romansh, "Cedel electoral senza indicaziun da la partida" },
    };

    internal static ElectionGroupBallotType ToEchElectionGroup(this DataModels.ProportionalElection proportionalElection)
    {
        var electionInfo = proportionalElection.ToEchElectionInformation();
        return ElectionGroupBallotType.Create(proportionalElection.DomainOfInfluenceId.ToString(), new[] { electionInfo });
    }

    internal static DataModels.ProportionalElection ToBasisProportionalElection(this ElectionInformationType election, IdLookup idLookup)
    {
        var electionIdentification = election.Election.ElectionIdentification;
        var id = idLookup.GuidForId(electionIdentification);

        var shortDescriptionInfos = election
            .Election
            .ElectionDescription
            ?.ElectionDescriptionInfo;
        var shortDescriptions = shortDescriptionInfos.ToLanguageDictionary(
            x => x.Language,
            x => x.ElectionDescriptionShort ?? UnknownMapping.UnknownValue,
            UnknownMapping.UnknownValue);

        var descriptionInfos = election
            .Election
            .ElectionDescription
            ?.ElectionDescriptionInfo;
        var descriptions = descriptionInfos.ToLanguageDictionary(
            x => x.Language,
            x => x.ElectionDescription ?? UnknownMapping.UnknownValue,
            UnknownMapping.UnknownValue);

        var alreadyTakenListPositions = election.List
            .Where(x => x.ListOrderOfPrecedence.HasValue)
            .Select(x => x.ListOrderOfPrecedence!.Value)
            .ToList();

        var electionInformationExtension = ElectionMapping.GetExtension(election.Extension?.Extension);

        var lists = election.List
            ?.Where(l => !l.IsEmptyList)
            .Select((l, i) => l.ToBasisList(id, idLookup, election.Candidate, electionInformationExtension?.Candidates, i + 1, alreadyTakenListPositions))
            .OrderBy(x => x.Position)
            .ToList()
            ?? new List<DataModels.ProportionalElectionList>();

        var listUnions = election.ListUnion
            ?.Select((lu, i) => lu.ToBasisListUnion(id, idLookup, i + 1))
            .ToList()
            ?? new List<DataModels.ProportionalElectionListUnion>();

        return new DataModels.ProportionalElection
        {
            Id = id,
            OfficialDescription = descriptions,
            ShortDescription = shortDescriptions,
            PoliticalBusinessNumber = election.Election.ElectionPosition.ToString(CultureInfo.InvariantCulture),
            NumberOfMandates = election.Election.NumberOfMandates,
            ProportionalElectionLists = lists,
            ProportionalElectionListUnions = listUnions,
            MandateAlgorithm = DataModels.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            BallotNumberGeneration = DataModels.BallotNumberGeneration.RestartForEachBundle,
            ReviewProcedure = DataModels.ProportionalElectionReviewProcedure.Electronically,
        };
    }

    private static ElectionInformationType ToEchElectionInformation(this DataModels.ProportionalElection proportionalElection)
    {
        var description = proportionalElection.ToEchElectionDescription();
        var electionType = ElectionType.Create(
            proportionalElection.Id.ToString(),
            TypeOfElectionType.Proporz,
            0,
            description,
            proportionalElection.NumberOfMandates,
            null);

        var canton = proportionalElection.Contest.DomainOfInfluence.CantonDefaults.Canton;

        var lists = proportionalElection.ProportionalElectionLists
            .OrderBy(l => l.Position)
            .Select(l => l.ToEchListType(canton))
            .Append(CreateEmptyListType(proportionalElection))
            .ToArray();

        var candidates = proportionalElection.ProportionalElectionLists
            .SelectMany(l => l.ProportionalElectionCandidates)
            .OrderBy(c => c.ProportionalElectionListId)
            .ThenBy(c => c.Number)
            .Select(c => c.ToEchProportionalCandidateType(canton))
            .ToArray();

        var listUnions = proportionalElection.ProportionalElectionListUnions
            .Where(u => u.ProportionalElectionListUnionEntries.Count > 0)
            .OrderBy(u => u.Position)
            .Select(ToEchListUnionType)
            .ToArray();

        return ElectionInformationType.Create(electionType, candidates, lists, listUnions);
    }

    private static ListType ToEchListType(this DataModels.ProportionalElectionList list, DataModels.DomainOfInfluenceCanton canton)
    {
        var descriptionInfos = new List<ListDescriptionInfo>();

        foreach (var (lang, desc) in list.Description)
        {
            list.ShortDescription.TryGetValue(lang, out var shortDescription);
            descriptionInfos.Add(ListDescriptionInfo.Create(lang, desc, shortDescription));
        }

        var description = ListDescriptionInformation.Create(descriptionInfos);

        var candidatePositions = new List<CandidatePositionInformation>();
        foreach (var candidate in list.ProportionalElectionCandidates.OrderBy(c => c.Position))
        {
            candidatePositions.Add(candidate.ToEchCandidatePosition(false, canton));
            if (candidate.Accumulated)
            {
                candidatePositions.Add(candidate.ToEchCandidatePosition(true, canton));
            }
        }

        return ListType.Create(
            list.Id.ToString(),
            list.OrderNumber,
            description,
            false,
            list.Position,
            list.ProportionalElectionCandidates.Sum(c => c.Accumulated ? 2 : 1),
            candidatePositions,
            list.BlankRowCount,
            null);
    }

    private static ListType CreateEmptyListType(DataModels.ProportionalElection proportionalElection)
    {
        var descriptionInfos = Languages.All
            .Select(language => ListDescriptionInfo.Create(
                language,
                EmptyListDescriptions.ContainsKey(language) ? EmptyListDescriptions[language] : EmptyListDescriptions[Languages.German],
                EmptyListShortDescription))
            .ToList();

        return ListType.Create(
            EmptyListId,
            EmptyListOrderNumber,
            ListDescriptionInformation.Create(descriptionInfos),
            true,
            EmptyListPosition,
            0,
            new List<CandidatePositionInformation>(),
            proportionalElection.NumberOfMandates,
            null);
    }

    private static DataModels.ProportionalElectionList ToBasisList(
        this ListType list,
        Guid electionId,
        IdLookup idLookup,
        CandidateType[] electionCandidates,
        List<ElectionInformationExtensionCandidate>? candidateExtensions,
        int position,
        ICollection<int> alreadyTakenListPositions)
    {
        var listId = idLookup.GuidForId(list.ListIdentification);

        var descriptionInfos = list
            .ListDescription
            ?.ListDescriptionInfo;
        var descriptions = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ListDescription, UnknownMapping.UnknownValue);

        var shortDescriptionInfos = list
            .ListDescription
            ?.ListDescriptionInfo;
        var shortDescriptions = shortDescriptionInfos.ToLanguageDictionary(
            x => x.Language,
            x => x.ListDescriptionShort ?? UnknownMapping.UnknownValue,
            UnknownMapping.UnknownValue);

        // The unknown value would be rejected by the input validation, while a single whitespace is allowed
        var orderNumber = list.ListIndentureNumber == UnknownMapping.UnknownValue
            ? " "
            : list.ListIndentureNumber;

        var candidates = list.CandidatePosition
            .ToBasisCandidates(listId, idLookup, electionCandidates, candidateExtensions)
            .OfType<DataModels.ProportionalElectionCandidate>()
            .ToList();

        return new DataModels.ProportionalElectionList
        {
            Id = listId,
            ProportionalElectionId = electionId,
            ProportionalElectionCandidates = candidates,
            BlankRowCount = list.EmptyListPositions ?? 0,
            OrderNumber = orderNumber,
            Position = GetListPosition(list.ListOrderOfPrecedence, position, alreadyTakenListPositions),
            Description = descriptions,
            ShortDescription = shortDescriptions,
        };
    }

    private static ListUnionTypeType ToEchListUnionType(this DataModels.ProportionalElectionListUnion listUnion)
    {
        var descriptionInfos = listUnion.Description
            .Select(x => ListUnionDescriptionInfoType.Create(x.Key, x.Value))
            .ToList();
        var description = ListUnionDescriptionType.Create(descriptionInfos);
        var relation = listUnion.IsSubListUnion ? ListRelationType.SubListUnion : ListRelationType.ListUnion;
        var listIds = listUnion.ProportionalElectionListUnionEntries
            .OrderBy(e => e.ProportionalElectionListId)
            .Select(e => e.ProportionalElectionListId.ToString())
            .ToList();

        var echListUnion = ListUnionTypeType.Create(listUnion.Id.ToString(), description, relation, listIds);
        echListUnion.ReferencedListUnion = listUnion.ProportionalElectionRootListUnionId?.ToString();
        return echListUnion;
    }

    private static DataModels.ProportionalElectionListUnion ToBasisListUnion(
        this ListUnionTypeType listUnion,
        Guid electionId,
        IdLookup idLookup,
        int position)
    {
        var listUnionId = idLookup.GuidForId(listUnion.ListUnionIdentification);
        var descriptionInfos = listUnion
            .ListUnionDescription
            ?.ListUnionDescriptionInfo;
        var description = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ListUnionDescription, UnknownMapping.UnknownValue);

        Guid? rootListUnionId = null;
        if (listUnion.ListUnionType == ListRelationType.SubListUnion)
        {
            if (string.IsNullOrEmpty(listUnion.ReferencedListUnion))
            {
                throw new ValidationException("Sub list union does not contain a referencedListUnion");
            }

            rootListUnionId = idLookup.GuidForId(listUnion.ReferencedListUnion);
        }

        return new DataModels.ProportionalElectionListUnion
        {
            Id = listUnionId,
            ProportionalElectionId = electionId,
            Description = description,
            ProportionalElectionRootListUnionId = rootListUnionId,
            ProportionalElectionListUnionEntries = listUnion.ReferencedList
                .Select(listId => new DataModels.ProportionalElectionListUnionEntry
                {
                    ProportionalElectionListUnionId = listUnionId,
                    ProportionalElectionListId = idLookup.GuidForId(listId),
                })
                .ToList(),
            Position = position,
        };
    }

    private static CandidateType ToEchProportionalCandidateType(this DataModels.ProportionalElectionCandidate candidate, DataModels.DomainOfInfluenceCanton canton)
    {
        var candidateType = candidate.ToEchCandidateType(candidate.Party?.ShortDescription, canton, PoliticalBusinessType.ProportionalElection);
        candidateType.CandidateReference = GenerateCandidateReference(candidate);
        return candidateType;
    }

    private static CandidatePositionInformation ToEchCandidatePosition(this DataModels.ProportionalElectionCandidate candidate, bool accumulatedPosition, DataModels.DomainOfInfluenceCanton canton)
    {
        var text = candidate.ToEchCandidateText(canton, PoliticalBusinessType.ProportionalElection, candidate.Party?.Name);
        var position = accumulatedPosition ? candidate.AccumulatedPosition : candidate.Position;
        return CandidatePositionInformation.Create(position, GenerateCandidateReference(candidate), candidate.Id.ToString(), text);
    }

    private static string GenerateCandidateReference(this DataModels.ProportionalElectionCandidate candidate)
    {
        return $"{candidate.ProportionalElectionList.OrderNumber.PadLeft(2, '0')}.{candidate.Number.PadLeft(2, '0')}";
    }

    private static IEnumerable<ProportionalElectionImportCandidate> ToBasisCandidates(
        this IReadOnlyCollection<CandidatePositionInformation> candidatePositions,
        Guid listId,
        IdLookup idLookup,
        CandidateType[] electionCandidates,
        List<ElectionInformationExtensionCandidate>? candidateExtensions)
    {
        var candidates = electionCandidates
            .Where(c => candidatePositions.Any(p => p.CandidateIdentification == c.CandidateIdentification))
            .Select(c => c.ToBasisProportionalElectionCandidate(idLookup, candidateExtensions?.FirstOrDefault(e => e.CandidateIdentification == c.CandidateIdentification)))
            .ToDictionary(c => c.Id);

        foreach (var posGroup in candidatePositions.GroupBy(p => p.CandidateIdentification))
        {
            var candidate = candidates[idLookup.GuidForId(posGroup.Key)];
            candidate.ProportionalElectionListId = listId;
            candidate.Position = posGroup.Min(x => x.PositionOnList);

            if (posGroup.Count() > 1)
            {
                candidate.Accumulated = true;
                candidate.AccumulatedPosition = posGroup.Max(x => x.PositionOnList);
            }
        }

        return candidates.Values.OrderBy(x => x.Position);
    }

    private static int GetNextFreeListPosition(ICollection<int> alreadyTakenListPositions)
    {
        return Enumerable.Range(1, alreadyTakenListPositions.Count + 1).Except(alreadyTakenListPositions).First();
    }

    private static int GetListPosition(int? listOrderOfPrecedence, int position, ICollection<int> alreadyTakenListPositions)
    {
        if (listOrderOfPrecedence.HasValue)
        {
            return listOrderOfPrecedence.Value;
        }

        var listPosition = alreadyTakenListPositions.Contains(position)
            ? GetNextFreeListPosition(alreadyTakenListPositions)
            : position;
        alreadyTakenListPositions.Add(listPosition);
        return listPosition;
    }

    private static ProportionalElectionImportCandidate ToBasisProportionalElectionCandidate(this CandidateType candidate, IdLookup idLookup, ElectionInformationExtensionCandidate? candidateExtension)
    {
        var basisCandidate = candidate.ToBasisCandidate<ProportionalElectionImportCandidate>(idLookup, candidateExtension);

        var partyInfo = candidate
            .PartyAffiliation
            ?.PartyAffiliationInfo;
        basisCandidate.SourcePartyShort = partyInfo?.ToOptionalLanguageDictionary(x => x.Language, x => x.PartyAffiliationShort);
        basisCandidate.SourceParty = partyInfo?.ToOptionalLanguageDictionary(x => x.Language, x => x.PartyAffiliation);
        return basisCandidate;
    }
}
