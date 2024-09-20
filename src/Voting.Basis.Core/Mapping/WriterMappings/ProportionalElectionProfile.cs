// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class ProportionalElectionProfile : Profile
{
    public ProportionalElectionProfile()
    {
        CreateMap<ProportionalElection, ProportionalElectionEventData>();
        CreateMap<ProportionalElectionEventData, ProportionalElectionAggregate>();

        CreateMap<ProportionalElectionCandidate, ProportionalElectionCandidateEventData>().ReverseMap();

        CreateMap<ProportionalElectionList, ProportionalElectionListEventData>().ReverseMap();
        CreateMap<ProportionalElectionListUnion, ProportionalElectionListUnionEventData>().ReverseMap();
        CreateMap<ProportionalElectionListUnionEntries, ProportionalElectionListUnionEntriesEventData>().ReverseMap();
        CreateMap<ProportionalElectionCandidateAfterTestingPhaseUpdated, ProportionalElectionCandidate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<ProportionalElectionAfterTestingPhaseUpdated, ProportionalElectionAggregate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<ProportionalElectionListAfterTestingPhaseUpdated, ProportionalElectionList>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
    }
}
