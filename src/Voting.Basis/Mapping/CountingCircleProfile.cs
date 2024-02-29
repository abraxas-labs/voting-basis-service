// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Voting.Basis.Data.Models;
using Authority = Voting.Basis.Core.Domain.Authority;
using CountingCircle = Voting.Basis.Core.Domain.CountingCircle;
using CountingCircleElectorate = Voting.Basis.Core.Domain.CountingCircleElectorate;
using CountingCirclesMerger = Voting.Basis.Core.Domain.CountingCirclesMerger;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public sealed class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        // write
        CreateMap<CreateCountingCircleRequest, CountingCircle>();
        CreateMap<UpdateCountingCircleRequest, CountingCircle>();
        CreateMap<ScheduleCountingCirclesMergerRequest, CountingCirclesMerger>()
            .ForMember(dst => dst.NewCountingCircle, opts => opts.MapFrom(src => src));
        CreateMap<ScheduleCountingCirclesMergerRequest, CountingCircle>();
        CreateMap<UpdateScheduledCountingCirclesMergerRequest, CountingCirclesMerger>()
            .ForMember(dst => dst.NewCountingCircle, opts => opts.MapFrom(src => src));
        CreateMap<UpdateScheduledCountingCirclesMergerRequest, CountingCircle>();
        CreateMap<ProtoModels.Authority, Authority>();
        CreateMap<ProtoModels.CountingCircleElectorate, CountingCircleElectorate>();

        // read
        CreateMap<IEnumerable<Data.Models.CountingCircle>, ProtoModels.CountingCircles>()
            .ForMember(dst => dst.CountingCircles_, opts => opts.MapFrom(src => src));
        CreateMap<Data.Models.CountingCircle, ProtoModels.CountingCircle>()
            .ForMember(dst => dst.Info, opts => opts.MapFrom(src => src));
        CreateMap<Data.Models.Authority, ProtoModels.Authority>();
        CreateMap<Data.Models.CountingCircleElectorate, ProtoModels.CountingCircleElectorate>();
        CreateMap<CountingCircleContactPerson, ProtoModels.ContactPerson>();

        CreateMap<DomainOfInfluenceCountingCircle, ProtoModels.CountingCircle>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.CountingCircleId))
            .IncludeMembers(x => x.CountingCircle);
        CreateMap<DomainOfInfluenceCountingCircle, ProtoModels.DomainOfInfluenceCountingCircle>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.CountingCircleId))
            .IncludeMembers(x => x.CountingCircle);

        CreateMap<IEnumerable<DomainOfInfluenceCountingCircle>, ProtoModels.DomainOfInfluenceCountingCircles>()
            .ForMember(dst => dst.CountingCircles, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.CountingCircle, ProtoModels.DomainOfInfluenceCountingCircle>();
        CreateMap<Data.Models.CountingCirclesMerger, ProtoModels.CountingCirclesMerger>();
        CreateMap<IEnumerable<Data.Models.CountingCirclesMerger>, ProtoModels.CountingCirclesMergers>()
            .ForMember(dst => dst.Mergers, opt => opt.MapFrom(x => x));
    }
}
