// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Import;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ImportProfile : Profile
{
    public ImportProfile()
    {
        // read
        CreateMap<ProtoModels.ContestImport, ContestImport>().ReverseMap();
        CreateMap<ProtoModels.Contest, Contest>().ReverseMap();
        CreateMap<ProtoModels.MajorityElectionImport, MajorityElectionImport>().ReverseMap();
        CreateMap<ProtoModels.MajorityElection, MajorityElection>().ReverseMap();
        CreateMap<ProtoModels.MajorityElectionCandidate, MajorityElectionCandidate>().ReverseMap();
        CreateMap<ProtoModels.MajorityElectionCandidateReference, MajorityElectionCandidateReference>().ReverseMap();
        CreateMap<ProtoModels.Ballot, Ballot>().ReverseMap();
        CreateMap<ProtoModels.BallotQuestion, BallotQuestion>().ReverseMap();
        CreateMap<ProtoModels.TieBreakQuestion, TieBreakQuestion>().ReverseMap();
        CreateMap<ProtoModels.ProportionalElectionImport, ProportionalElectionImport>().ReverseMap();
        CreateMap<ProtoModels.ProportionalElectionListImport, ProportionalElectionListImport>().ReverseMap();
        CreateMap<ProtoModels.ProportionalElection, ProportionalElection>().ReverseMap();
        CreateMap<ProtoModels.ProportionalElectionListUnion, ProportionalElectionListUnion>().ReverseMap();
        CreateMap<ProtoModels.ProportionalElectionList, ProportionalElectionList>().ReverseMap();
        CreateMap<ProportionalElectionImportCandidate, ProtoModels.ProportionalElectionImportCandidate>()
            .ForMember(dst => dst.Candidate, opts => opts.MapFrom(src => src))
            .ReverseMap();
        CreateMap<ProtoModels.ProportionalElectionCandidate, ProportionalElectionImportCandidate>().ReverseMap();
        CreateMap<ProtoModels.VoteImport, VoteImport>().ReverseMap();
        CreateMap<ProtoModels.Vote, Vote>().ReverseMap();
    }
}
