// (c) Copyright by Abraxas Informatik AG
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

        CreateMap<MajorityElectionCandidate, MajorityElectionCandidateEventData>()
            .ForMember(dst => dst.Party, opts => opts.MapFrom(src => src.PartyShortDescription))
            .ReverseMap();

        CreateMap<SecondaryMajorityElection, SecondaryMajorityElectionEventData>().ReverseMap();
        CreateMap<MajorityElectionCandidateReference, MajorityElectionCandidateReferenceEventData>().ReverseMap();
        CreateMap<MajorityElectionCandidateReference, MajorityElectionCandidateReference>();
        CreateMap<MajorityElectionBallotGroup, MajorityElectionBallotGroupEventData>().ReverseMap();
        CreateMap<MajorityElectionBallotGroupEntry, MajorityElectionBallotGroupEntryEventData>().ReverseMap();

        CreateMap<MajorityElectionBallotGroupCandidates, MajorityElectionBallotGroupCandidatesEventData>().ReverseMap();
        CreateMap<MajorityElectionBallotGroupEntryCandidates, MajorityElectionBallotGroupEntryCandidatesEventData>().ReverseMap();

        CreateMap<SecondaryMajorityElectionAfterTestingPhaseUpdated, SecondaryMajorityElection>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<MajorityElectionCandidateAfterTestingPhaseUpdated, MajorityElectionCandidate>(MemberList.Source)
            .ForMember(dst => dst.PartyShortDescription, opts => opts.MapFrom(src => src.Party))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated, MajorityElectionCandidate>(MemberList.Source)
            .ForMember(dst => dst.PartyShortDescription, opts => opts.MapFrom(src => src.Party))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
    }
}
