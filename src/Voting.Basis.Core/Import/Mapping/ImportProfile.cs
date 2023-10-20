// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Import.Mapping;

public class ImportProfile : Profile
{
    public ImportProfile()
    {
        CreateMap<Contest, ContestImport>()
            .ForMember(dst => dst.Contest, opts => opts.MapFrom(x => x));
        CreateMap<Contest, Domain.Contest>();
        CreateMap<MajorityElection, MajorityElectionImport>()
            .ForMember(dst => dst.Election, opts => opts.MapFrom(x => x))
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(x => x.MajorityElectionCandidates));
        CreateMap<MajorityElection, Domain.MajorityElection>();
        CreateMap<MajorityElectionCandidate, Domain.MajorityElectionCandidate>();
        CreateMap<ProportionalElection, ProportionalElectionImport>()
            .ForMember(dst => dst.Election, opts => opts.MapFrom(x => x))
            .ForMember(dst => dst.Lists, opts => opts.MapFrom(x => x.ProportionalElectionLists))
            .ForMember(dst => dst.ListUnions, opts => opts.MapFrom(x => x.ProportionalElectionListUnions));
        CreateMap<ProportionalElection, Domain.ProportionalElection>();
        CreateMap<ProportionalElectionList, ProportionalElectionListImport>()
            .ForMember(dst => dst.List, opts => opts.MapFrom(x => x))
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(x => x.ProportionalElectionCandidates.Cast<Ech.Models.ProportionalElectionImportCandidate>()));
        CreateMap<ProportionalElectionList, Domain.ProportionalElectionList>();
        CreateMap<ProportionalElectionListUnion, Domain.ProportionalElectionListUnion>()
            .ForMember(dst => dst.ProportionalElectionListIds, opts => opts.MapFrom(src => src.ProportionalElectionListUnionEntries.Select(x => x.ProportionalElectionListId)));
        CreateMap<Ech.Models.ProportionalElectionImportCandidate, ProportionalElectionImportCandidate>();
        CreateMap<Vote, VoteImport>()
            .ForMember(dst => dst.Vote, opts => opts.MapFrom(x => x));
        CreateMap<Vote, Domain.Vote>();
        CreateMap<Ballot, Domain.Ballot>();
        CreateMap<BallotQuestion, Domain.BallotQuestion>();
        CreateMap<TieBreakQuestion, Domain.TieBreakQuestion>();
    }
}
