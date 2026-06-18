// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Voting.Basis.Core.Models;
using Voting.Basis.Data.Models;
using Contest = Voting.Basis.Core.Domain.Contest;
using PoliticalBusiness = Voting.Basis.Data.Models.PoliticalBusiness;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public sealed class ContestProfile : Profile
{
    public ContestProfile()
    {
        // write
        CreateMap<CreateContestRequest, Contest>();
        CreateMap<UpdateContestRequest, Contest>();

        // read
        CreateMap<Data.Models.Contest, ProtoModels.Contest>()
            .ForMember(dst => dst.PoliticalBusinesses, opts => opts.MapFrom(src => src.SimplePoliticalBusinesses));
        CreateMap<IEnumerable<Data.Models.Contest>, ProtoModels.Contests>()
            .ForMember(dst => dst.Contests_, opts => opts.MapFrom(src => src));
        CreateMap<ContestSummaryEntryDetails, ProtoModels.ContestSummaryEntryDetails>();
        CreateMap<ContestSummary, ProtoModels.ContestSummary>()
            .IncludeMembers(src => src.Contest);
        CreateMap<IEnumerable<ContestSummary>, ProtoModels.ContestSummaries>()
            .ForMember(dst => dst.ContestSummaries_, opts => opts.MapFrom(src => src));

        CreateMap<PoliticalBusinessSummary, ProtoModels.PoliticalBusinessSummary>()
            .IncludeMembers(src => src.PoliticalBusiness);
        CreateMap<IEnumerable<PoliticalBusinessSummary>, ProtoModels.PoliticalBusinessSummaries>()
            .ForMember(dst => dst.PoliticalBusinessSummaries_, opts => opts.MapFrom(src => src));

        CreateMap<IEnumerable<PoliticalBusiness>, ProtoModels.PoliticalBusinesses>()
            .ForMember(dst => dst.PoliticalBusinesses_, opts => opts.MapFrom(src => src));

        CreateMap<PoliticalBusiness, ProtoModels.PoliticalBusiness>();
        CreateMap<PoliticalBusinessUnion, ProtoModels.PoliticalBusinessUnion>();

        CreateMap<Data.Models.Contest, ProtoModels.ContestSummary>();
        CreateMap<PoliticalBusiness, ProtoModels.PoliticalBusinessSummary>();

        CreateMap<SimplePoliticalBusiness, ProtoModels.PoliticalBusiness>();
    }
}
