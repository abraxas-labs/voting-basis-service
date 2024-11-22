// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Ech0155_4_0;
using Ech0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Utils;
using Voting.Basis.Ech.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech.Ech0157_4_0.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class ProportionalElectionMapping
{
    private const string EmptyListOrderNumber = "99";
    private const string EmptyListPosition = "99";
    private const string EmptyListShortDescription = "WoP";
    private static readonly Dictionary<string, string> EmptyListDescriptions = new()
    {
        { Languages.German, "Wahlzettel ohne Parteibezeichnung" },
        { Languages.French, "Bulletin de vote sans désignation de parti" },
        { Languages.Italian, "Scheda elettorale senza indicazione del partito" },
        { Languages.Romansh, "Cedel electoral senza indicaziun da la partida" },
    };

    internal static EventInitialDeliveryElectionGroupBallot ToEchElectionGroup(this ProportionalElection proportionalElection)
    {
        var electionInfo = proportionalElection.ToEchElectionInformation();
        var electionGroupBallot = new EventInitialDeliveryElectionGroupBallot
        {
            DomainOfInfluenceIdentification = proportionalElection.DomainOfInfluenceId.ToString(),
            ElectionInformation = new List<EventInitialDeliveryElectionGroupBallotElectionInformation> { electionInfo },
        };

        return electionGroupBallot;
    }

    internal static ProportionalElection ToBasisProportionalElection(this EventInitialDeliveryElectionGroupBallotElectionInformation election, IdLookup idLookup)
    {
        var electionIdentification = election.Election.ElectionIdentification;
        var id = idLookup.GuidForId(electionIdentification);

        var shortDescriptionInfos = election
            .Election
            .ElectionDescription;
        var shortDescriptions = shortDescriptionInfos.ToLanguageDictionary(
            x => x.Language,
            x => x.ElectionDescriptionShort ?? UnknownMapping.UnknownValue,
            UnknownMapping.UnknownValue);

        var descriptionInfos = election
            .Election
            .ElectionDescription;
        var descriptions = descriptionInfos.ToLanguageDictionary(
            x => x.Language,
            x => x.ElectionDescription ?? UnknownMapping.UnknownValue,
            UnknownMapping.UnknownValue);

        var alreadyTakenListPositions = election.List
            .Where(x => !string.IsNullOrEmpty(x.ListOrderOfPrecedence))
            .Select(x => int.Parse(x.ListOrderOfPrecedence))
            .ToList();

        var electionInformationExtension = ElectionMapping.GetExtension(election.Extension?.Any);

        var lists = election.List
            ?.Where(l => !l.IsEmptyList)
            .Select((l, i) => l.ToBasisList(id, idLookup, election.Candidate.ToArray(), electionInformationExtension?.Candidates, i + 1, alreadyTakenListPositions))
            .OrderBy(x => x.Position)
            .ToList()
            ?? new List<ProportionalElectionList>();

        var listUnions = election.ListUnion
            ?.Select((lu, i) => lu.ToBasisListUnion(id, idLookup, i + 1))
            .ToList()
            ?? new List<ProportionalElectionListUnion>();

        return new ProportionalElection
        {
            Id = id,
            OfficialDescription = descriptions,
            ShortDescription = shortDescriptions,
            PoliticalBusinessNumber = election.Election.ElectionPosition.ToString(CultureInfo.InvariantCulture),
            NumberOfMandates = int.Parse(election.Election.NumberOfMandates),
            ProportionalElectionLists = lists,
            ProportionalElectionListUnions = listUnions,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
        };
    }

    private static EventInitialDeliveryElectionGroupBallotElectionInformation ToEchElectionInformation(this ProportionalElection proportionalElection)
    {
        var description = proportionalElection.ToEchElectionDescription();
        var electionType = new ElectionType
        {
            ElectionIdentification = proportionalElection.Id.ToString(),
            TypeOfElection = TypeOfElectionType.Item1,
            ElectionPosition = "0",
            ElectionDescription = description.ElectionDescriptionInfo,
            NumberOfMandates = proportionalElection.NumberOfMandates.ToString(),
        };

        var canton = proportionalElection.Contest.DomainOfInfluence.CantonDefaults.Canton;

        var lists = proportionalElection.ProportionalElectionLists
            .OrderBy(l => l.Position)
            .Select(l => l.ToEchListType(canton))
            .Append(CreateEmptyListType(proportionalElection))
            .ToList();

        var candidates = proportionalElection.ProportionalElectionLists
            .SelectMany(l => l.ProportionalElectionCandidates)
            .OrderBy(c => c.ProportionalElectionListId)
            .ThenBy(c => c.Number)
            .Select(c => c.ToEchProportionalCandidateType(canton))
            .ToList();

        var listUnions = proportionalElection.ProportionalElectionListUnions
            .Where(u => u.ProportionalElectionListUnionEntries.Count > 0)
            .OrderBy(u => u.Position)
            .Select(ToEchListUnionType)
            .ToList();

        return new EventInitialDeliveryElectionGroupBallotElectionInformation
        {
            Election = electionType,
            Candidate = candidates,
            List = lists,
            ListUnion = listUnions,
        };
    }

    private static ListType ToEchListType(this ProportionalElectionList list, DomainOfInfluenceCanton canton)
    {
        var descriptionInfos = new ListDescriptionInformationType();

        foreach (var (lang, desc) in list.Description)
        {
            list.ShortDescription.TryGetValue(lang, out var shortDescription);
            descriptionInfos.ListDescriptionInfo.Add(new ListDescriptionInformationTypeListDescriptionInfo
            {
                Language = lang,
                ListDescription = desc,
                ListDescriptionShort = shortDescription,
            });
        }

        var candidatePositions = new List<CandidatePositionInformationType>();
        foreach (var candidate in list.ProportionalElectionCandidates.OrderBy(c => c.Position))
        {
            candidatePositions.Add(candidate.ToEchCandidatePosition(false, canton));
            if (candidate.Accumulated)
            {
                candidatePositions.Add(candidate.ToEchCandidatePosition(true, canton));
            }
        }

        return new ListType
        {
            ListIdentification = list.Id.ToString(),
            ListIndentureNumber = list.OrderNumber,
            ListDescription = descriptionInfos.ListDescriptionInfo,
            IsEmptyList = false,
            ListOrderOfPrecedence = list.Position.ToString(),
            TotalPositionsOnList = list.ProportionalElectionCandidates.Sum(c => c.Accumulated ? 2 : 1).ToString(),
            CandidatePosition = candidatePositions,
            EmptyListPositions = list.BlankRowCount.ToString(),
            ListUnionBallotText = null,
        };
    }

    private static ListType CreateEmptyListType(ProportionalElection proportionalElection)
    {
        var descriptionInfos = Languages.All
            .Select(language => new ListDescriptionInformationTypeListDescriptionInfo
            {
                Language = language,
                ListDescription = EmptyListDescriptions.TryGetValue(language, out var description)
                    ? description
                    : EmptyListDescriptions[Languages.German],
                ListDescriptionShort = EmptyListShortDescription,
            })
            .ToList();

        return new ListType
        {
            ListIdentification = BasisUuidV5.BuildProportionalElectionEmptyList(proportionalElection.Id).ToString(),
            ListIndentureNumber = EmptyListOrderNumber,
            ListDescription = descriptionInfos,
            IsEmptyList = true,
            ListOrderOfPrecedence = EmptyListPosition,
            TotalPositionsOnList = "0",
            EmptyListPositions = proportionalElection.NumberOfMandates.ToString(),
            ListUnionBallotText = null,
        };
    }

    private static ProportionalElectionList ToBasisList(
        this ListType list,
        Guid electionId,
        IdLookup idLookup,
        CandidateType[] electionCandidates,
        List<ElectionInformationExtensionCandidate>? candidateExtensions,
        int position,
        ICollection<int> alreadyTakenListPositions)
    {
        var listId = idLookup.GuidForId(list.ListIdentification);

        var descriptionInfos = list.ListDescription;
        var descriptions = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ListDescription, UnknownMapping.UnknownValue);
        var shortDescriptionInfos = list.ListDescription;
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
            .OfType<ProportionalElectionCandidate>()
            .ToList();

        return new ProportionalElectionList
        {
            Id = listId,
            ProportionalElectionId = electionId,
            ProportionalElectionCandidates = candidates,
            BlankRowCount = string.IsNullOrEmpty(list.EmptyListPositions) ? 0 : int.Parse(list.EmptyListPositions),
            OrderNumber = orderNumber,
            Position = GetListPosition(list.ListOrderOfPrecedence, position, alreadyTakenListPositions),
            Description = descriptions,
            ShortDescription = shortDescriptions,
        };
    }

    private static ListUnionType ToEchListUnionType(this ProportionalElectionListUnion listUnion)
    {
        var descriptionInfos = listUnion.Description
            .Select(x => new ListUnionDescriptionTypeListUnionDescriptionInfo
            {
                Language = x.Key,
                ListUnionDescription = x.Value,
            })
            .ToList();
        var relation = listUnion.IsSubListUnion ? ListRelationType.Item2 : ListRelationType.Item1;
        var listIds = listUnion.ProportionalElectionListUnionEntries
            .OrderBy(e => e.ProportionalElectionListId)
            .Select(e => e.ProportionalElectionListId.ToString())
            .ToList();

        return new ListUnionType
        {
            ListUnionIdentification = listUnion.Id.ToString(),
            ListUnionDescription = descriptionInfos,
            ListUnionTypeProperty = relation,
            ReferencedList = listIds,
            ReferencedListUnion = listUnion.ProportionalElectionRootListUnionId?.ToString(),
        };
    }

    private static ProportionalElectionListUnion ToBasisListUnion(
        this ListUnionType listUnion,
        Guid electionId,
        IdLookup idLookup,
        int position)
    {
        var listUnionId = idLookup.GuidForId(listUnion.ListUnionIdentification);
        var descriptionInfos = listUnion.ListUnionDescription;
        var description = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ListUnionDescription, UnknownMapping.UnknownValue);

        Guid? rootListUnionId = null;
        if (listUnion.ListUnionTypeProperty == ListRelationType.Item2)
        {
            if (string.IsNullOrEmpty(listUnion.ReferencedListUnion))
            {
                throw new ValidationException("Sub list union does not contain a referencedListUnion");
            }

            rootListUnionId = idLookup.GuidForId(listUnion.ReferencedListUnion);
        }

        return new ProportionalElectionListUnion
        {
            Id = listUnionId,
            ProportionalElectionId = electionId,
            Description = description,
            ProportionalElectionRootListUnionId = rootListUnionId,
            ProportionalElectionListUnionEntries = listUnion.ReferencedList
                .ConvertAll(listId => new ProportionalElectionListUnionEntry
                {
                    ProportionalElectionListUnionId = listUnionId,
                    ProportionalElectionListId = idLookup.GuidForId(listId),
                }),
            Position = position,
        };
    }

    private static CandidateType ToEchProportionalCandidateType(this ProportionalElectionCandidate candidate, DomainOfInfluenceCanton canton)
    {
        var candidateType = candidate.ToEchCandidateType(candidate.Party?.ShortDescription, canton, PoliticalBusinessType.ProportionalElection);
        candidateType.CandidateReference = GenerateCandidateReference(candidate);
        return candidateType;
    }

    private static CandidatePositionInformationType ToEchCandidatePosition(this ProportionalElectionCandidate candidate, bool accumulatedPosition, DomainOfInfluenceCanton canton)
    {
        var text = candidate.ToEchCandidateText(canton, PoliticalBusinessType.ProportionalElection, candidate.Party?.Name);
        var position = accumulatedPosition ? candidate.AccumulatedPosition : candidate.Position;
        return new CandidatePositionInformationType
        {
            PositionOnList = position.ToString(),
            CandidateReferenceOnPosition = GenerateCandidateReference(candidate),
            CandidateIdentification = candidate.Id.ToString(),
            CandidateTextOnPosition = text.CandidateTextInfo,
        };
    }

    private static string GenerateCandidateReference(this ProportionalElectionCandidate candidate)
    {
        return $"{candidate.ProportionalElectionList.OrderNumber.PadLeft(2, '0')}.{candidate.Number.PadLeft(2, '0')}";
    }

    private static IEnumerable<ProportionalElectionImportCandidate> ToBasisCandidates(
        this IReadOnlyCollection<CandidatePositionInformationType> candidatePositions,
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
            candidate.Position = posGroup.Min(x => int.Parse(x.PositionOnList));

            if (posGroup.Count() > 1)
            {
                candidate.Accumulated = true;
                candidate.AccumulatedPosition = posGroup.Max(x => int.Parse(x.PositionOnList));
            }
        }

        return candidates.Values.OrderBy(x => x.Position);
    }

    private static int GetNextFreeListPosition(ICollection<int> alreadyTakenListPositions)
    {
        return Enumerable.Range(1, alreadyTakenListPositions.Count + 1).Except(alreadyTakenListPositions).First();
    }

    private static int GetListPosition(string listOrderOfPrecedence, int position, ICollection<int> alreadyTakenListPositions)
    {
        if (!string.IsNullOrEmpty(listOrderOfPrecedence))
        {
            return int.Parse(listOrderOfPrecedence);
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

        var partyInfo = candidate.PartyAffiliation;
        basisCandidate.SourcePartyShort = partyInfo?.ToOptionalLanguageDictionary(x => x.Language, x => x.PartyAffiliationShort);
        basisCandidate.SourceParty = partyInfo?.ToOptionalLanguageDictionary(x => x.Language, x => x.PartyAffiliationLong);
        return basisCandidate;
    }
}
