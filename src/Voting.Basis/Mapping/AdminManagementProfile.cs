// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class AdminManagementProfile : Profile
{
    public AdminManagementProfile()
    {
        // read
        CreateMap<IEnumerable<Data.Models.DomainOfInfluence>, ProtoModels.PoliticalDomainOfInfluenceHierarchies>()
            .ForMember(dst => dst.PoliticalDomainOfInfluences, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.DomainOfInfluence, ProtoModels.PoliticalDomainOfInfluence>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.ParentId, opts => opts.MapFrom(src => src.ParentId))
            .ForMember(dst => dst.Children, opts => opts.MapFrom(src => src.Children))
            .ForMember(dst => dst.Name, opts => opts.MapFrom(src => src.Name))
            .ForMember(dst => dst.Bfs, opts => opts.MapFrom(src => src.Bfs))
            .ForMember(dst => dst.TenantName, opts => opts.MapFrom(src => src.AuthorityName))
            .ForMember(dst => dst.TenantId, opts => opts.MapFrom(src => src.SecureConnectId))
            .ForMember(dst => dst.Type, opts => opts.MapFrom(src => src.Type))
            .ForMember(dst => dst.Canton, opts => opts.MapFrom(src => src.Canton))
            .ForMember(dst => dst.ReturnAddress, opts => opts.MapFrom(src => src.ReturnAddress));
    }
}
