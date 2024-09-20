// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Voting.Basis.Core.Messaging.Messages;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ElectionGroupProfile : Profile
{
    public ElectionGroupProfile()
    {
        // write
        CreateMap<UpdateElectionGroupRequest, Core.Domain.ElectionGroup>();

        // read
        CreateMap<Data.Models.ElectionGroup, ProtoModels.ElectionGroup>()
            .ForMember(dst => dst.SecondaryElectionIds, opts => opts.MapFrom(src => src.SecondaryMajorityElections.Select(sme => sme.Id)))
            .ForMember(dst => dst.SecondaryPoliticalBusinessNumbers, opts => opts.MapFrom(src => src.SecondaryMajorityElections.Select(sme => sme.PoliticalBusinessNumber)));

        CreateMap<IEnumerable<Data.Models.ElectionGroup>, ProtoModels.ElectionGroups>()
            .ForMember(dst => dst.ElectionGroups_, opts => opts.MapFrom(src => src));

        CreateMap<BaseEntityMessage<Data.Models.ElectionGroup>, ProtoModels.ElectionGroupMessage>();
    }
}
