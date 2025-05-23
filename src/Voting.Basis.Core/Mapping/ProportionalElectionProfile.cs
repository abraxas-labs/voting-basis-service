﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
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
        CreateMap<ProportionalElectionCandidateAfterTestingPhaseUpdated, ProportionalElectionCandidate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
    }
}
