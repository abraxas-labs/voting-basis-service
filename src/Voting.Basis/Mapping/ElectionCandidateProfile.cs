// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ElectionCandidateProfile : Profile
{
    public ElectionCandidateProfile()
    {
        // read
        CreateMap<IEnumerable<Data.Models.ElectionCandidate>, ProtoModels.ElectionCandidates>()
            .ForMember(dst => dst.ElectionCandidates_, opts => opts.MapFrom(src => src));
        CreateMap<Data.Models.ElectionCandidate, ProtoModels.ElectionCandidate>();
    }
}
