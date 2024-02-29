// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public sealed class ContestProfile : Profile
{
    public ContestProfile()
    {
        CreateMap<ContestEventData, Contest>()
            .ForMember(dst => dst.PastLockPer, opts => opts.MapFrom(src => src.Date.ToDateTime().NextUtcDate(true)));

        CreateMap<PreconfiguredContestDateEventData, PreconfiguredContestDate>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.Date));
    }
}
