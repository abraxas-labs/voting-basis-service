// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Data.Models;
using Authority = Voting.Basis.Data.Models.Authority;
using ContactPerson = Voting.Basis.Core.Domain.ContactPerson;
using CountingCircle = Voting.Basis.Core.Domain.CountingCircle;
using CountingCirclesMerger = Voting.Basis.Core.Domain.CountingCirclesMerger;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class CountingCircleTestProfile : Profile
{
    public CountingCircleTestProfile()
    {
        CreateMap<ProtoModels.CountingCircle, CountingCircleAggregate>();
        CreateMap<ProtoModels.CountingCirclesMerger, CountingCirclesMergerEventData>()
            .ForMember(dst => dst.MergedCountingCircleIds, opts => opts.MapFrom(src => src.MergedCountingCircles.Select(x => x.Id)));
        CreateMap<ProtoModels.CountingCirclesMerger, CountingCirclesMerger>()
            .ForMember(dst => dst.MergedCountingCircleIds, opts => opts.MapFrom(src => src.MergedCountingCircles.Select(x => x.Id)));
        CreateMap<ProtoModels.CountingCircle, CountingCircle>();
        CreateMap<ProtoModels.CountingCircle, CountingCircleEventData>();
        CreateMap<ProtoModels.Authority, AuthorityEventData>();
        CreateMap<ProtoModels.ContactPerson, ContactPersonEventData>();

        CreateMap<Data.Models.CountingCircle, CountingCircle>();
        CreateMap<Authority, Core.Domain.Authority>();
        CreateMap<CountingCircleContactPerson, ContactPerson>();
    }
}
