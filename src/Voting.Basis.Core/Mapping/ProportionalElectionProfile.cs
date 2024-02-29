// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public class ProportionalElectionProfile : Profile
{
    public ProportionalElectionProfile()
    {
        CreateMap<ProportionalElectionAfterTestingPhaseUpdated, ProportionalElection>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<ProportionalElectionListAfterTestingPhaseUpdated, ProportionalElectionList>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<ProportionalElectionEventData, ProportionalElection>();
        CreateMap<ProportionalElectionCandidateEventData, ProportionalElectionCandidate>();
        CreateMap<ProportionalElectionListEventData, ProportionalElectionList>();
        CreateMap<ProportionalElectionUnionEventData, ProportionalElectionUnion>();
        CreateMap<ProportionalElectionListUnionEventData, ProportionalElectionListUnion>();
        CreateMap<ProportionalElectionUnion, SimplePoliticalBusinessUnion>()
            .ForMember(dst => dst.UnionType, opts => opts.MapFrom(src => src.Type));
        CreateMap<ProportionalElectionCandidateAfterTestingPhaseUpdated, ProportionalElectionCandidate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
    }
}
