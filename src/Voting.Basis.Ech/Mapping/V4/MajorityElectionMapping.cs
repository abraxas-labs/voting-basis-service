// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ech0155_4_0;
using Ech0157_4_0;
using Voting.Basis.Data.Models;
using Voting.Lib.Ech.Ech0157_4_0.Models;

namespace Voting.Basis.Ech.Mapping.V4;

internal static class MajorityElectionMapping
{
    internal static EventInitialDeliveryElectionGroupBallot ToEchElectionGroup(this MajorityElection majorityElection, bool eVoting)
    {
        var electionInfos = majorityElection.SecondaryMajorityElections
            .OrderBy(s => s.PoliticalBusinessNumber)
            .Select(s => ToEchElectionInformation(s, eVoting))
            .Append(majorityElection.ToEchElectionInformation(eVoting))
            .ToList();

        return new EventInitialDeliveryElectionGroupBallot
        {
            DomainOfInfluenceIdentification = majorityElection.DomainOfInfluenceId.ToString(),
            ElectionInformation = electionInfos,
        };
    }

    internal static EventInitialDeliveryElectionGroupBallotElectionInformation ToEchElectionInformation(this MajorityElection majorityElection, bool eVoting)
    {
        var description = majorityElection.ToEchElectionDescription(eVoting);
        var referencedElections = majorityElection.SecondaryMajorityElections
            .OrderBy(s => s.PoliticalBusinessNumber)
            .Select(ToEchReferenceElection)
            .ToList();
        var electionType = new ElectionType
        {
            ElectionIdentification = majorityElection.Id.ToString(),
            TypeOfElection = TypeOfElectionType.Item2,
            ElectionPosition = "0",
            ElectionDescription = description.ElectionDescriptionInfo,
            NumberOfMandates = majorityElection.NumberOfMandates.ToString(),
            ReferencedElection = referencedElections,
        };

        var candidates = majorityElection.MajorityElectionCandidates
            .OrderBy(c => c.Number)
            .Select(c => c.ToEchCandidateType(c.Party, majorityElection.Contest.DomainOfInfluence.CantonDefaults.Canton, eVoting, PoliticalBusinessType.MajorityElection))
            .ToList();

        return new EventInitialDeliveryElectionGroupBallotElectionInformation
        {
            Election = electionType,
            Candidate = candidates,
        };
    }

    internal static MajorityElection ToBasisMajorityElection(this EventInitialDeliveryElectionGroupBallotElectionInformation election, IdLookup idLookup)
    {
        var electionIdentification = election.Election.ElectionIdentification;
        var electionId = idLookup.GuidForId(electionIdentification);

        var officialDescriptionInfos = election
            .Election
            .ElectionDescription;
        var officialDescriptions = officialDescriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ElectionDescription ?? electionIdentification, electionIdentification);

        var shortDescriptionInfos = election
            .Election
            .ElectionDescription;
        var shortDescriptions = shortDescriptionInfos.ToLanguageDictionary(x => x.Language, x => x.ElectionDescriptionShort ?? electionIdentification, electionIdentification);

        var electionInformationExtension = ElectionMapping.GetExtension(election.Extension?.Any);

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

        return new MajorityElection
        {
            Id = electionId,
            OfficialDescription = officialDescriptions,
            ShortDescription = shortDescriptions,
            PoliticalBusinessNumber = election.Election.ElectionPosition.ToString(CultureInfo.InvariantCulture),
            NumberOfMandates = int.Parse(election.Election.NumberOfMandates),
            MajorityElectionCandidates = candidates,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
        };
    }

    private static EventInitialDeliveryElectionGroupBallotElectionInformation ToEchElectionInformation(this SecondaryMajorityElection secondaryElection, bool eVoting)
    {
        var description = secondaryElection.ToEchElectionDescription(eVoting);

        var referencedElection = secondaryElection.PrimaryMajorityElection.ToEchReferenceElection();
        var electionType = new ElectionType
        {
            ElectionIdentification = secondaryElection.Id.ToString(),
            TypeOfElection = TypeOfElectionType.Item2,
            ElectionPosition = "0",
            ElectionDescription = description.ElectionDescriptionInfo,
            NumberOfMandates = secondaryElection.NumberOfMandates.ToString(),
            ReferencedElection = new List<ReferencedElectionInformationType> { referencedElection },
        };

        var candidates = secondaryElection.Candidates
            .OrderBy(c => c.Number)
            .Select(c => c.ToEchCandidateType(c.Party, secondaryElection.Contest.DomainOfInfluence.CantonDefaults.Canton, secondaryElection.Contest.EVoting, PoliticalBusinessType.SecondaryMajorityElection))
            .ToList();

        return new EventInitialDeliveryElectionGroupBallotElectionInformation()
        {
            Election = electionType,
            Candidate = candidates,
        };
    }

    private static ReferencedElectionInformationType ToEchReferenceElection(this SecondaryMajorityElection secondaryElection)
    {
        return new ReferencedElectionInformationType
        {
            ReferencedElection = secondaryElection.Id.ToString(),
            ElectionRelation = ElectionRelationType.Item2,
        };
    }

    private static ReferencedElectionInformationType ToEchReferenceElection(this MajorityElection election)
    {
        return new ReferencedElectionInformationType
        {
            ReferencedElection = election.Id.ToString(),
            ElectionRelation = ElectionRelationType.Item1,
        };
    }

    private static MajorityElectionCandidate ToBasisMajorityCandidate(this CandidateType candidate, Guid electionId, IdLookup idLookup, ElectionInformationExtensionCandidate? candidateExtension)
    {
        var basisCandidate = candidate.ToBasisCandidate<MajorityElectionCandidate>(idLookup, candidateExtension);

        basisCandidate.MajorityElectionId = electionId;
        var partyInfos = candidate.PartyAffiliation;
        if (partyInfos.Count > 0)
        {
            basisCandidate.Party = partyInfos.ToLanguageDictionary(x => x.Language, x => x.PartyAffiliationShort ?? x.PartyAffiliationLong, string.Empty);
        }

        return basisCandidate;
    }
}
