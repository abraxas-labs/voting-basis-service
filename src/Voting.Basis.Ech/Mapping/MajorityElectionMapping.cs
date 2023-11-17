// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using eCH_0155_4_0;
using eCH_0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Lib.Ech.Ech0157.Models;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class MajorityElectionMapping
{
    internal static ElectionGroupBallotType ToEchElectionGroup(this DataModels.MajorityElection majorityElection)
    {
        var electionInfos = majorityElection.SecondaryMajorityElections
            .OrderBy(s => s.PoliticalBusinessNumber)
            .Select(ToEchElectionInformation)
            .Append(majorityElection.ToEchElectionInformation())
            .ToArray();

        return ElectionGroupBallotType.Create(majorityElection.DomainOfInfluenceId.ToString(), electionInfos);
    }

    internal static ElectionInformationType ToEchElectionInformation(this DataModels.MajorityElection majorityElection)
    {
        var description = majorityElection.ToEchElectionDescription();
        var referencedElections = majorityElection.SecondaryMajorityElections
            .OrderBy(s => s.PoliticalBusinessNumber)
            .Select(ToEchReferenceElection)
            .ToList();
        var electionType = ElectionType.Create(
            majorityElection.Id.ToString(),
            TypeOfElectionType.Majorz,
            0,
            description,
            majorityElection.NumberOfMandates,
            referencedElections);

        var candidates = majorityElection.MajorityElectionCandidates
            .OrderBy(c => c.Number)
            .Select(c => c.ToEchCandidateType(c.Party, majorityElection.Contest.DomainOfInfluence.CantonDefaults.Canton, PoliticalBusinessType.MajorityElection))
            .ToArray();

        return ElectionInformationType.Create(electionType, candidates);
    }

    internal static DataModels.MajorityElection ToBasisMajorityElection(this ElectionInformationType election, IdLookup idLookup)
    {
        var electionIdentification = election.Election.ElectionIdentification;
        var electionId = idLookup.GuidForId(electionIdentification);

        var officialDescriptionInfos = election
            .Election
            .ElectionDescription
            ?.ElectionDescriptionInfo;
        var officialDescriptions = officialDescriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ElectionDescription ?? electionIdentification, electionIdentification);

        var shortDescriptionInfos = election
            .Election
            .ElectionDescription
            ?.ElectionDescriptionInfo;
        var shortDescriptions = shortDescriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ElectionDescriptionShort ?? electionIdentification, electionIdentification);

        var electionInformationExtension = ElectionMapping.GetExtension(election.Extension?.Extension);

        var candidates = (election.Candidate ?? Enumerable.Empty<CandidateType>())
            .Select(c => c.ToBasisMajorityCandidate(
                electionId,
                idLookup,
                electionInformationExtension?.Candidates?.FirstOrDefault(e => e.CandidateIdentification == c.CandidateIdentification)))
            .ToList();

        for (var i = 0; i < candidates.Count; i++)
        {
            candidates[i].Position = i + 1;
        }

        return new DataModels.MajorityElection
        {
            Id = electionId,
            OfficialDescription = officialDescriptions,
            ShortDescription = shortDescriptions,
            PoliticalBusinessNumber = election.Election.ElectionPosition.ToString(CultureInfo.InvariantCulture),
            NumberOfMandates = election.Election.NumberOfMandates,
            MajorityElectionCandidates = candidates,
            ResultEntry = DataModels.MajorityElectionResultEntry.Detailed,
            MandateAlgorithm = DataModels.MajorityElectionMandateAlgorithm.AbsoluteMajority,
            BallotNumberGeneration = DataModels.BallotNumberGeneration.RestartForEachBundle,
            ReviewProcedure = DataModels.MajorityElectionReviewProcedure.Electronically,
        };
    }

    private static ElectionInformationType ToEchElectionInformation(this DataModels.SecondaryMajorityElection secondaryElection)
    {
        var description = secondaryElection.ToEchElectionDescription();

        var referencedElection = secondaryElection.PrimaryMajorityElection.ToEchReferenceElection();
        var electionType = ElectionType.Create(
            secondaryElection.Id.ToString(),
            TypeOfElectionType.Majorz,
            0,
            description,
            secondaryElection.NumberOfMandates,
            new List<ReferencedElection> { referencedElection });

        var candidates = secondaryElection.Candidates
            .OrderBy(c => c.Number)
            .Select(c => c.ToEchCandidateType(c.Party, secondaryElection.Contest.DomainOfInfluence.CantonDefaults.Canton, PoliticalBusinessType.SecondaryMajorityElection))
            .ToArray();

        return ElectionInformationType.Create(electionType, candidates);
    }

    private static ReferencedElection ToEchReferenceElection(this DataModels.SecondaryMajorityElection secondaryElection)
    {
        return ReferencedElection.Create(secondaryElection.Id.ToString(), ElectionRelationType.Minor);
    }

    private static ReferencedElection ToEchReferenceElection(this DataModels.MajorityElection election)
    {
        return ReferencedElection.Create(election.Id.ToString(), ElectionRelationType.Major);
    }

    private static DataModels.MajorityElectionCandidate ToBasisMajorityCandidate(this CandidateType candidate, Guid electionId, IdLookup idLookup, ElectionInformationExtensionCandidate? candidateExtension)
    {
        var basisCandidate = candidate.ToBasisCandidate<DataModels.MajorityElectionCandidate>(idLookup, candidateExtension);

        basisCandidate.MajorityElectionId = electionId;
        var partyInfos = candidate
            .PartyAffiliation
            ?.PartyAffiliationInfo;
        basisCandidate.Party = partyInfos.ToLanguageDictionary(x => x.Language, x => x.PartyAffiliationShort ?? x.PartyAffiliation, string.Empty);

        return basisCandidate;
    }
}
