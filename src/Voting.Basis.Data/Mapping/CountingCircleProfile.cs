// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Data.Mapping;

public class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        CreateMap<CountingCircle, CountingCircleSnapshot>()
            .ForMember(dst => dst.Id, opts => opts.Ignore())
            .ForMember(dst => dst.BasisId, opts => opts.MapFrom(src => src.Id));

        // the ids for childs of cc will be generated automatically
        CreateMap<CountingCircleContactPerson, CountingCircleContactPersonSnapshot>()
            .ForMember(dst => dst.Id, opts => opts.Ignore())
            .ForMember(dst => dst.CountingCircleAfterEventId, opts => opts.Ignore())
            .ForMember(dst => dst.CountingCircleDuringEventId, opts => opts.Ignore());

        CreateMap<Authority, AuthoritySnapshot>()
            .ForMember(dst => dst.Id, opts => opts.Ignore())
            .ForMember(dst => dst.CountingCircleId, opts => opts.Ignore());
    }
}
