// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping.SnapshotMapping;

public class DomainOfInfluenceSnapshotProfile : Profile
{
    public DomainOfInfluenceSnapshotProfile()
    {
        CreateMap<IEnumerable<Data.Models.Snapshots.DomainOfInfluenceSnapshot>, ProtoModels.DomainOfInfluences>()
            .ForMember(dst => dst.DomainOfInfluences_, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.Snapshots.DomainOfInfluenceSnapshot, ProtoModels.DomainOfInfluence>()
            .ForMember(dst => dst.Info, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.BasisId))
            .ForMember(dst => dst.ParentId, opts => opts.MapFrom(src => src.BasisParentId));

        CreateMap<Data.Models.Snapshots.DomainOfInfluenceSnapshot, ProtoModels.EntityInfo>()
            .IncludeBase<Data.Models.Snapshots.ISnapshotEntity, ProtoModels.EntityInfo>();
    }
}
