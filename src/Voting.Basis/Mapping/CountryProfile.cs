// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using Voting.Basis.Ech.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class CountryProfile : Profile
{
    public CountryProfile()
    {
        CreateMap<IEnumerable<Country>, ProtoModels.Countries>()
            .ForMember(dst => dst.Countries_, opts => opts.MapFrom(src => src));

        CreateMap<Country, ProtoModels.Country>();
    }
}
