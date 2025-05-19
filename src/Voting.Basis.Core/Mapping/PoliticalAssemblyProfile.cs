// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public sealed class PoliticalAssemblyProfile : Profile
{
    public PoliticalAssemblyProfile()
    {
        CreateMap<PoliticalAssemblyEventData, PoliticalAssembly>()
            .ForMember(dst => dst.PastLockPer, opts => opts.MapFrom(src => src.Date.ToDateTime().NextUtcDate(true)));
    }
}
