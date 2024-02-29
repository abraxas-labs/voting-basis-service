// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Data.Mapping;

public class DomainOfInfluenceProfile : Profile
{
    public DomainOfInfluenceProfile()
    {
        CreateMap<DomainOfInfluence, DomainOfInfluenceSnapshot>()
            .ForMember(dst => dst.Id, opts => opts.Ignore())
            .ForMember(dst => dst.BasisId, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.BasisParentId, opts => opts.MapFrom(src => src.ParentId));

        CreateMap<DomainOfInfluenceCountingCircle, DomainOfInfluenceCountingCircleSnapshot>()
            .ForMember(dst => dst.Id, opts => opts.Ignore())
            .ForMember(dst => dst.BasisId, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.BasisCountingCircleId, opts => opts.MapFrom(src => src.CountingCircleId))
            .ForMember(dst => dst.BasisDomainOfInfluenceId, opts => opts.MapFrom(src => src.DomainOfInfluenceId));
    }
}
