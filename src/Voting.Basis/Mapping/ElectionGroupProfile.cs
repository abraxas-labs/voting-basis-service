﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ElectionGroupProfile : Profile
{
    public ElectionGroupProfile()
    {
        // read
        CreateMap<Data.Models.ElectionGroup, ProtoModels.ElectionGroup>()
            .ForMember(dst => dst.SecondaryElectionIds, opts => opts.MapFrom(src => src.SecondaryMajorityElections.Select(sme => sme.Id)))
            .ForMember(dst => dst.SecondaryPoliticalBusinessNumbers, opts => opts.MapFrom(src => src.SecondaryMajorityElections.Select(sme => sme.PoliticalBusinessNumber)));
    }
}
