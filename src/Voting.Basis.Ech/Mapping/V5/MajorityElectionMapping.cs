// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0155_5_1;
using Ech0157_5_1;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping.V5;

internal static class MajorityElectionMapping
{
    internal static EventInitialDeliveryElectionGroup ToEchElectionGroup(this MajorityElection majorityElection, bool eVoting)
    {
        var electionPosition = 1;

        var primaryElectionInfo = majorityElection.ToEchElectionInformation(electionPosition++, eVoting);

        var electionInfos = majorityElection.SecondaryMajorityElections
            .OrderBy(s => s.PoliticalBusinessNumber)
            .Select(s => ToEchElectionInformation(s, electionPosition++, eVoting))
            .Append(primaryElectionInfo)
            .ToList();

        return new EventInitialDeliveryElectionGroup
        {
            DomainOfInfluence = majorityElection.DomainOfInfluence!.ToEchDomainOfInfluence(),
            ElectionInformation = electionInfos,
        };
    }

    internal static EventInitialDeliveryElectionGroupElectionInformation ToEchElectionInformation(this MajorityElection majorityElection, int electionPosition, bool eVoting)
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
            ElectionPosition = electionPosition.ToString(),
            ElectionDescription = description.ElectionDescriptionInfo,
            NumberOfMandates = majorityElection.NumberOfMandates.ToString(),
            ReferencedElection = referencedElections,
        };

        var candidates = majorityElection.MajorityElectionCandidates
            .OrderBy(c => c.Number)
            .Select(c => c.ToEchCandidateType(c.PartyShortDescription, c.PartyLongDescription, majorityElection.Contest.DomainOfInfluence.CantonDefaults.Canton, eVoting, PoliticalBusinessType.MajorityElection))
            .ToList();

        return new EventInitialDeliveryElectionGroupElectionInformation
        {
            Election = electionType,
            Candidate = candidates,
        };
    }

    private static EventInitialDeliveryElectionGroupElectionInformation ToEchElectionInformation(this SecondaryMajorityElection secondaryElection, int electionPosition, bool eVoting)
    {
        var description = secondaryElection.ToEchElectionDescription(eVoting);

        var referencedElection = secondaryElection.PrimaryMajorityElection.ToEchReferenceElection();
        var electionType = new ElectionType
        {
            ElectionIdentification = secondaryElection.Id.ToString(),
            TypeOfElection = TypeOfElectionType.Item2,
            ElectionPosition = electionPosition.ToString(),
            ElectionDescription = description.ElectionDescriptionInfo,
            NumberOfMandates = secondaryElection.NumberOfMandates.ToString(),
            ReferencedElection = new List<ReferencedElectionInformationType> { referencedElection },
        };

        var candidates = secondaryElection.Candidates
            .OrderBy(c => c.Number)
            .Select(c => c.ToEchCandidateType(c.PartyShortDescription, c.PartyLongDescription, secondaryElection.Contest.DomainOfInfluence.CantonDefaults.Canton, eVoting, PoliticalBusinessType.SecondaryMajorityElection))
            .ToList();

        return new EventInitialDeliveryElectionGroupElectionInformation()
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
}
