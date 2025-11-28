// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public class MajorityElectionProfile : Profile
{
    public MajorityElectionProfile()
    {
        CreateMap<MajorityElectionEventData, MajorityElection>();
        CreateMap<SecondaryMajorityElectionEventData, SecondaryMajorityElection>();
        CreateMap<MajorityElectionAfterTestingPhaseUpdated, MajorityElection>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<MajorityElectionAfterTestingPhaseUpdated, MajorityElectionAggregate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<MajorityElectionCandidateReferenceEventData, SecondaryMajorityElectionCandidate>();
        CreateMap<MajorityElectionCandidateEventData, MajorityElectionCandidate>()
            .ForMember(dst => dst.PartyShortDescription, opts => opts.MapFrom(src => src.Party));
        CreateMap<MajorityElectionBallotGroupEntryEventData, MajorityElectionBallotGroupEntry>()
            .ForMember(dst => dst.SecondaryMajorityElectionId, opts => opts.MapFrom(src => src.ElectionId));
        CreateMap<MajorityElectionBallotGroupEventData, MajorityElectionBallotGroup>()
            .AfterMap((_, ballotGroup) =>
            {
                var primaryElectionId = ballotGroup.MajorityElectionId;
                var primaryElectionEntry = ballotGroup.Entries.FirstOrDefault(e => e.SecondaryMajorityElectionId == primaryElectionId);
                if (primaryElectionEntry != null)
                {
                    primaryElectionEntry.PrimaryMajorityElectionId = primaryElectionId;
                    primaryElectionEntry.SecondaryMajorityElectionId = null;
                }
            });

        CreateMap<MajorityElectionCandidateAfterTestingPhaseUpdated, MajorityElectionCandidate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate())
            .ForMember(dst => dst.PartyShortDescription, opts => opts.MapFrom(src => src.Party));
        CreateMap<SecondaryMajorityElectionAfterTestingPhaseUpdated, SecondaryMajorityElection>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<MajorityElectionCandidateEventData, SecondaryMajorityElectionCandidate>()
            .ForMember(dst => dst.SecondaryMajorityElectionId, opts => opts.MapFrom(src => src.MajorityElectionId))
            .ForMember(dst => dst.PartyShortDescription, opts => opts.MapFrom(src => src.Party));
        CreateMap<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated, SecondaryMajorityElectionCandidate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate())
            .ForMember(dst => dst.PartyShortDescription, opts => opts.MapFrom(src => src.Party));
        CreateMap<MajorityElectionCandidate, SecondaryMajorityElectionCandidate>()
            .ForMember(dst => dst.CandidateReferenceId, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.BallotGroupEntries, opts => opts.Ignore());
    }
}
