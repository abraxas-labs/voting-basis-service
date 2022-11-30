// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using eCH_0155_4_0;
using eCH_0157_4_0;
using Voting.Lib.Common;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class ProportionalElectionMapping
{
    private const string EmptyListId = "99";
    private const string EmptyListOrderNumber = "99";
    private const int EmptyListPosition = 99;
    private const string EmptyListShortDescription = "WoP";
    private const string EmptyListDescription = "Wahlzettel ohne Parteibezeichnung";

    internal static ElectionGroupBallotType ToEchElectionGroup(this DataModels.ProportionalElection proportionalElection)
    {
        var electionInfo = proportionalElection.ToEchElectionInformation();
        return ElectionGroupBallotType.Create(proportionalElection.DomainOfInfluenceId.ToString(), new[] { electionInfo });
    }

    internal static DataModels.ProportionalElection ToBasisProportionalElection(this ElectionInformationType election, IdLookup idLookup)
    {
        string electionIdentification = election.Election.ElectionIdentification;
        var id = idLookup.GuidForId(electionIdentification);

        var shortDescriptionInfos = election
            .Election
            .ElectionDescription
            ?.ElectionDescriptionInfo;
        var shortDescriptions = shortDescriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ElectionDescriptionShort ?? electionIdentification, electionIdentification);

        var descriptionInfos = election
            .Election
            .ElectionDescription
            ?.ElectionDescriptionInfo;
        var descriptions = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ElectionDescription ?? electionIdentification, electionIdentification);

        var lists = election.List
            ?.Where(l => !l.IsEmptyList)
            ?.Select(l => l.ToBasisList(id, idLookup, election.Candidate))
            ?.ToList()
            ?? new List<DataModels.ProportionalElectionList>();

        var listUnions = election.ListUnion
            ?.Select(lu => lu.ToBasisListUnion(id, idLookup))
            ?.ToList()
            ?? new List<DataModels.ProportionalElectionListUnion>();
        for (var i = 0; i < listUnions.Count; i++)
        {
            listUnions[i].Position = i + 1;
        }

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

        var lists = proportionalElection.ProportionalElectionLists
            .OrderBy(l => l.Position)
            .Select(ToEchListType)
            .Append(CreateEmptyListType(proportionalElection))
            .ToArray();
        var candidates = proportionalElection.ProportionalElectionLists
            .SelectMany(l => l.ProportionalElectionCandidates)
            .OrderBy(c => c.ProportionalElectionListId)
            .ThenBy(c => c.Number)
            .Select(c => c.ToEchProportionalCandidateType())
            .ToArray();

        var listUnions = proportionalElection.ProportionalElectionListUnions
            .Where(u => u.ProportionalElectionListUnionEntries.Count > 0)
            .OrderBy(u => u.Position)
            .Select(ToEchListUnionType)
            .ToArray();

        return ElectionInformationType.Create(electionType, candidates, lists, listUnions);
    }

    private static ListType ToEchListType(this DataModels.ProportionalElectionList list)
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
            candidatePositions.Add(candidate.ToEchCandidatePosition(false));
            if (candidate.Accumulated)
            {
                candidatePositions.Add(candidate.ToEchCandidatePosition(true));
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
            .Select(x => ListDescriptionInfo.Create(x, EmptyListDescription, EmptyListShortDescription))
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
        CandidateType[] electionCandidates)
    {
        var listId = idLookup.GuidForId(list.ListIdentification);

        var descriptionInfos = list
            .ListDescription
            ?.ListDescriptionInfo;
        var descriptions = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ListDescription, list.ListIdentification);

        var shortDescriptionInfos = list
            .ListDescription
            ?.ListDescriptionInfo;
        var shortDescriptions = shortDescriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ListDescriptionShort ?? list.ListIdentification, list.ListIdentification);

        return new DataModels.ProportionalElectionList
        {
            Id = listId,
            ProportionalElectionId = electionId,
            ProportionalElectionCandidates = list.CandidatePosition.ToBasisCandidates(listId, idLookup, electionCandidates),
            BlankRowCount = list.EmptyListPositions ?? 0,
            OrderNumber = list.ListIndentureNumber,
            Position = list.ListOrderOfPrecedence ?? 0,
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
        return ListUnionTypeType.Create(listUnion.Id.ToString(), description, relation, listIds);
    }

    private static DataModels.ProportionalElectionListUnion ToBasisListUnion(this ListUnionTypeType listUnion, Guid electionId, IdLookup idLookup)
    {
        var listUnionId = idLookup.GuidForId(listUnion.ListUnionIdentification);
        var descriptionInfos = listUnion
            .ListUnionDescription
            ?.ListUnionDescriptionInfo;
        var description = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ListUnionDescription, listUnion.ListUnionIdentification);

        Guid? listElectionRootId = listUnion.ListUnionType == ListRelationType.SubListUnion
            ? idLookup.GuidForId(listUnion.ReferencedList[0])
            : (Guid?)null;

        return new DataModels.ProportionalElectionListUnion
        {
            Id = listUnionId,
            ProportionalElectionId = electionId,
            Description = description,
            ProportionalElectionRootListUnionId = listElectionRootId,
            ProportionalElectionListUnionEntries = listUnion.ReferencedList
                .Select(listId => new DataModels.ProportionalElectionListUnionEntry
                {
                    ProportionalElectionListUnionId = listUnionId,
                    ProportionalElectionListId = idLookup.GuidForId(listId),
                })
                .ToList(),
        };
    }

    private static CandidateType ToEchProportionalCandidateType(this DataModels.ProportionalElectionCandidate candidate)
    {
        var candidateType = candidate.ToEchCandidateType();
        candidateType.CandidateReference = GenerateCandidateReference(candidate);
        return candidateType;
    }

    private static CandidatePositionInformation ToEchCandidatePosition(this DataModels.ProportionalElectionCandidate candidate, bool accumulatedPosition)
    {
        var text = candidate.ToEchCandidateText();
        var position = accumulatedPosition ? candidate.AccumulatedPosition : candidate.Position;
        return CandidatePositionInformation.Create(position, GenerateCandidateReference(candidate), candidate.Id.ToString(), text);
    }

    private static string GenerateCandidateReference(this DataModels.ProportionalElectionCandidate candidate)
    {
        return $"{candidate.ProportionalElectionList.OrderNumber.PadLeft(2, '0')}.{candidate.Number.PadLeft(2, '0')}";
    }

    private static List<DataModels.ProportionalElectionCandidate> ToBasisCandidates(
        this List<CandidatePositionInformation> candidatePositions,
        Guid listId,
        IdLookup idLookup,
        CandidateType[] electionCandidates)
    {
        var candidates = electionCandidates
            .Where(c => candidatePositions.Any(p => p.CandidateIdentification == c.CandidateIdentification))
            .Select(c => c.ToBasisCandidate<DataModels.ProportionalElectionCandidate>(idLookup))
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

        return candidates.Values.ToList();
    }
}
