// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class MajorityElectionProfile : Profile
{
    public MajorityElectionProfile()
    {
        CreateMap<MajorityElection, MajorityElectionEventData>();
        CreateMap<MajorityElectionEventData, MajorityElectionAggregate>();
        CreateMap<MajorityElectionCandidate, MajorityElectionCandidateEventData>().ReverseMap();

        CreateMap<SecondaryMajorityElection, SecondaryMajorityElectionEventData>().ReverseMap();
        CreateMap<MajorityElectionCandidateReference, MajorityElectionCandidateReferenceEventData>().ReverseMap();
        CreateMap<MajorityElectionBallotGroup, MajorityElectionBallotGroupEventData>().ReverseMap();
        CreateMap<MajorityElectionBallotGroupEntry, MajorityElectionBallotGroupEntryEventData>().ReverseMap();

        CreateMap<MajorityElectionBallotGroupCandidates, MajorityElectionBallotGroupCandidatesEventData>().ReverseMap();
        CreateMap<MajorityElectionBallotGroupEntryCandidates, MajorityElectionBallotGroupEntryCandidatesEventData>().ReverseMap();

        CreateMap<MajorityElectionCandidateAfterTestingPhaseUpdated, MajorityElectionCandidate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<SecondaryMajorityElectionAfterTestingPhaseUpdated, Domain.SecondaryMajorityElection>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated, MajorityElectionCandidate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
    }
}
