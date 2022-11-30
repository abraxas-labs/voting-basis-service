// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.Mapping;

public class PoliticalBusinessProfile : Profile
{
    public PoliticalBusinessProfile()
    {
        CreateMap<PoliticalBusiness, SimplePoliticalBusiness>()
            .ForMember(dst => dst.BusinessType, opt => opt.MapFrom(src => src.PoliticalBusinessType));
    }
}
