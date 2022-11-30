// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class DomainOfInfluencePartyProfile : Profile
{
    public DomainOfInfluencePartyProfile()
    {
        // read
        CreateMap<IEnumerable<Data.Models.DomainOfInfluenceParty>, ProtoModels.DomainOfInfluenceParties>()
            .ForMember(dst => dst.Parties, opts => opts.MapFrom(src => src));
        CreateMap<Data.Models.DomainOfInfluenceParty, ProtoModels.DomainOfInfluenceParty>();
    }
}
