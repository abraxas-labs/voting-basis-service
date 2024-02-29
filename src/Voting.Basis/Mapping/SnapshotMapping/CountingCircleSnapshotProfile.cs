// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping.SnapshotMapping;

public class CountingCircleSnapshotProfile : Profile
{
    public CountingCircleSnapshotProfile()
    {
        CreateMap<IEnumerable<Data.Models.Snapshots.CountingCircleSnapshot>, ProtoModels.CountingCircles>()
            .ForMember(dst => dst.CountingCircles_, opts => opts.MapFrom(src => src));

        CreateMap<IEnumerable<Data.Models.Snapshots.DomainOfInfluenceCountingCircleSnapshot>, ProtoModels.DomainOfInfluenceCountingCircles>()
            .ForMember(dst => dst.CountingCircles, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.Snapshots.CountingCircleSnapshot, ProtoModels.CountingCircle>()
            .ForMember(dst => dst.Info, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.BasisId));

        CreateMap<Data.Models.Snapshots.AuthoritySnapshot, ProtoModels.Authority>();
        CreateMap<Data.Models.Snapshots.CountingCircleContactPersonSnapshot, ProtoModels.ContactPerson>();

        CreateMap<Data.Models.Snapshots.CountingCircleSnapshot, ProtoModels.EntityInfo>()
            .IncludeBase<Data.Models.Snapshots.ISnapshotEntity, ProtoModels.EntityInfo>();

        CreateMap<Data.Models.Snapshots.DomainOfInfluenceCountingCircleSnapshot, ProtoModels.DomainOfInfluenceCountingCircle>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.BasisCountingCircleId))
            .IncludeMembers(x => x.CountingCircle);
        CreateMap<Data.Models.Snapshots.CountingCircleSnapshot, ProtoModels.DomainOfInfluenceCountingCircle>();
    }
}
