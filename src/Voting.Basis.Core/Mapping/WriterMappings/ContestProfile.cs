// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class ContestProfile : Profile
{
    public ContestProfile()
    {
        CreateMap<Contest, ContestEventData>();
        CreateMap<ContestEventData, ContestAggregate>()
            .ForMember(dst => dst.PastLockPer, opts => opts.MapFrom(src => src.Date.ToDateTime().NextUtcDate(true)));
    }
}
