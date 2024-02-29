// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Messaging.Messages;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public sealed class ContestProfile : Profile
{
    public ContestProfile()
    {
        // write
        CreateMap<CreateContestRequest, Contest>();
        CreateMap<UpdateContestRequest, Contest>();
        CreateMap<UpdateCountingCircleOptionRequest, ContestCountingCircleOption>();

        // read
        CreateMap<Data.Models.Contest, ProtoModels.Contest>()
            .ForMember(dst => dst.PoliticalBusinesses, opts => opts.MapFrom(src => src.SimplePoliticalBusinesses));
        CreateMap<IEnumerable<Data.Models.Contest>, ProtoModels.Contests>()
            .ForMember(dst => dst.Contests_, opts => opts.MapFrom(src => src));
        CreateMap<Core.Models.ContestSummaryEntryDetails, ProtoModels.ContestSummaryEntryDetails>();
        CreateMap<Core.Models.ContestSummary, ProtoModels.ContestSummary>()
            .IncludeMembers(src => src.Contest);
        CreateMap<IEnumerable<Core.Models.ContestSummary>, ProtoModels.ContestSummaries>()
            .ForMember(dst => dst.ContestSummaries_, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.PreconfiguredContestDate, ProtoModels.PreconfiguredContestDate>()
            .ForMember(dst => dst.Date, opts => opts.MapFrom(src => src.Id));
        CreateMap<IEnumerable<Data.Models.PreconfiguredContestDate>, ProtoModels.PreconfiguredContestDates>()
            .ForMember(dst => dst.PreconfiguredContestDates_, opts => opts.MapFrom(src => src));
        CreateMap<IEnumerable<Data.Models.PoliticalBusiness>, ProtoModels.PoliticalBusinesses>()
            .ForMember(dst => dst.PoliticalBusinesses_, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.PoliticalBusiness, ProtoModels.PoliticalBusiness>();
        CreateMap<Data.Models.PoliticalBusinessUnion, ProtoModels.PoliticalBusinessUnion>();

        CreateMap<IEnumerable<Data.Models.ContestCountingCircleOption>, ProtoModels.ContestCountingCircleOptions>()
            .ForMember(dst => dst.Options, opts => opts.MapFrom(src => src));
        CreateMap<Data.Models.ContestCountingCircleOption, ProtoModels.ContestCountingCircleOption>();

        CreateMap<Data.Models.Contest, ProtoModels.ContestSummary>();

        CreateMap<Data.Models.SimplePoliticalBusiness, ProtoModels.PoliticalBusiness>();
        CreateMap<SimplePoliticalBusinessUnion, ProtoModels.PoliticalBusinessUnion>();

        CreateMap<ContestDetailsChangeMessage, ProtoModels.ContestDetailsChangeMessage>();
        CreateMap<BaseEntityMessage<Data.Models.Contest>, ProtoModels.ContestMessage>();
        CreateMap<ContestOverviewChangeMessage, ProtoModels.ContestOverviewChangeMessage>();
        CreateMap<BaseEntityMessage<Data.Models.SimplePoliticalBusiness>, ProtoModels.PoliticalBusinessMessage>();
        CreateMap<BaseEntityMessage<SimplePoliticalBusinessUnion>, ProtoModels.PoliticalBusinessUnionMessage>();
    }
}
