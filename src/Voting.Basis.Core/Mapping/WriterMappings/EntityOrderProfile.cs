// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class EntityOrderProfile : Profile
{
    public EntityOrderProfile()
    {
        CreateMap<Domain.EntityOrder, EntityOrderEventData>();
        CreateMap<IEnumerable<Domain.EntityOrder>, EntityOrdersEventData>()
            .ForMember(dst => dst.Orders, opts => opts.MapFrom(src => src));
    }
}
