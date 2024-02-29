// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class SnapshotProfile : Profile
{
    public SnapshotProfile()
    {
        CreateMap<Data.Models.Snapshots.ISnapshotEntity, ProtoModels.EntityInfo>()
            .ForMember(dst => dst.ModifiedOn, opts => opts.MapFrom(src => src.ValidFrom))
            .ForMember(dst => dst.DeletedOn, opts => opts.MapFrom(src => src.Deleted ? (DateTime?)src.ValidFrom : null));

        CreateMap(typeof(Data.Models.Snapshots.IHasSnapshotEntity<>), typeof(ProtoModels.EntityInfo))
            .ForMember(nameof(ProtoModels.EntityInfo.DeletedOn), opts => opts.Ignore());
    }
}
