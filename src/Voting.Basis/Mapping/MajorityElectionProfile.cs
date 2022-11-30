// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class MajorityElectionProfile : Profile
{
    public MajorityElectionProfile()
    {
        // write
        CreateMap<CreateMajorityElectionRequest, Core.Domain.MajorityElection>();
        CreateMap<UpdateMajorityElectionRequest, Core.Domain.MajorityElection>();
        CreateMap<CreateMajorityElectionCandidateRequest, Core.Domain.MajorityElectionCandidate>();
        CreateMap<UpdateMajorityElectionCandidateRequest, Core.Domain.MajorityElectionCandidate>();
        CreateMap<CreateSecondaryMajorityElectionRequest, Core.Domain.SecondaryMajorityElection>();
        CreateMap<UpdateSecondaryMajorityElectionRequest, Core.Domain.SecondaryMajorityElection>();

        CreateMap<CreateSecondaryMajorityElectionCandidateRequest, Core.Domain.MajorityElectionCandidate>()
            .ForMember(dst => dst.MajorityElectionId, opts => opts.MapFrom(src => src.SecondaryMajorityElectionId));
        CreateMap<UpdateSecondaryMajorityElectionCandidateRequest, Core.Domain.MajorityElectionCandidate>()
            .ForMember(dst => dst.MajorityElectionId, opts => opts.MapFrom(src => src.SecondaryMajorityElectionId));

        CreateMap<CreateMajorityElectionCandidateReferenceRequest, Core.Domain.MajorityElectionCandidateReference>();
        CreateMap<UpdateMajorityElectionCandidateReferenceRequest, Core.Domain.MajorityElectionCandidateReference>();

        CreateMap<CreateMajorityElectionBallotGroupRequest, Core.Domain.MajorityElectionBallotGroup>();
        CreateMap<UpdateMajorityElectionBallotGroupRequest, Core.Domain.MajorityElectionBallotGroup>();

        CreateMap<UpdateMajorityElectionBallotGroupCandidatesRequest, Core.Domain.MajorityElectionBallotGroupCandidates>();
        CreateMap<ProtoModels.MajorityElectionBallotGroupEntry, Core.Domain.MajorityElectionBallotGroupEntry>();
        CreateMap<ProtoModels.MajorityElectionBallotGroupEntryCandidates, Core.Domain.MajorityElectionBallotGroupEntryCandidates>();

        // read
        CreateMap<Data.Models.MajorityElection, ProtoModels.MajorityElection>();
        CreateMap<IEnumerable<Data.Models.MajorityElection>, ProtoModels.MajorityElections>()
            .ForMember(dst => dst.MajorityElections_, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.MajorityElectionCandidate, ProtoModels.MajorityElectionCandidate>();
        CreateMap<IEnumerable<Data.Models.MajorityElectionCandidate>, ProtoModels.MajorityElectionCandidates>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.SecondaryMajorityElection, ProtoModels.SecondaryMajorityElection>();
        CreateMap<IEnumerable<Data.Models.SecondaryMajorityElection>, ProtoModels.SecondaryMajorityElections>()
            .ForMember(dst => dst.SecondaryMajorityElections_, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.SecondaryMajorityElectionCandidate, ProtoModels.MajorityElectionCandidate>()
            .ForMember(dst => dst.MajorityElectionId, opts => opts.MapFrom(src => src.SecondaryMajorityElectionId));
        CreateMap<Data.Models.SecondaryMajorityElectionCandidate, ProtoModels.SecondaryMajorityElectionCandidate>()
            .ForMember(dst => dst.Candidate, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.IsReferenced, opts => opts.MapFrom(src => src.CandidateReferenceId.HasValue))
            .ForMember(dst => dst.ReferencedCandidateId, opts => opts.MapFrom(src => src.CandidateReferenceId == null ? string.Empty : src.CandidateReferenceId.ToString()));
        CreateMap<IEnumerable<Data.Models.SecondaryMajorityElectionCandidate>, ProtoModels.SecondaryMajorityElectionCandidates>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(src => src));

        CreateMap<Core.Domain.MajorityElectionBallotGroup, ProtoModels.MajorityElectionBallotGroup>();
        CreateMap<Core.Domain.MajorityElectionBallotGroupEntry, ProtoModels.MajorityElectionBallotGroupEntry>();
        CreateMap<Data.Models.MajorityElectionBallotGroup, ProtoModels.MajorityElectionBallotGroup>();
        CreateMap<Data.Models.MajorityElectionBallotGroupEntry, ProtoModels.MajorityElectionBallotGroupEntry>()
            .ForMember(dst => dst.ElectionId, opts => opts.MapFrom(src => src.PrimaryMajorityElectionId ?? src.SecondaryMajorityElectionId));
        CreateMap<IEnumerable<Data.Models.MajorityElectionBallotGroup>, ProtoModels.MajorityElectionBallotGroups>()
            .ForMember(dst => dst.BallotGroups, opts => opts.MapFrom(src => src));
        CreateMap<Data.Models.MajorityElectionBallotGroupEntry, ProtoModels.MajorityElectionBallotGroupEntryCandidates>()
            .ForMember(dst => dst.BallotGroupEntryId, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.CandidateIds, opts => opts.MapFrom(src => src.Candidates.Select(
                c => c.PrimaryElectionCandidateId.HasValue ? c.PrimaryElectionCandidateId.ToString() : c.SecondaryElectionCandidateId.ToString())));
        CreateMap<IEnumerable<Data.Models.MajorityElectionBallotGroupEntry>, ProtoModels.MajorityElectionBallotGroupCandidates>()
            .ForMember(dst => dst.EntryCandidates, opts => opts.MapFrom(src => src));
    }
}
