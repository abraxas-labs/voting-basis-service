// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public class MajorityElectionUnionProfile : Profile
{
    public MajorityElectionUnionProfile()
    {
        CreateMap<MajorityElectionUnion, SimplePoliticalBusinessUnion>()
            .ForMember(dst => dst.UnionType, opts => opts.MapFrom(src => src.Type))
            .ForMember(dst => dst.PoliticalBusinessIds, opts => opts.MapFrom(src => src.MajorityElectionUnionEntries.Select(x => x.MajorityElectionUnionId)));
        CreateMap<MajorityElectionUnionEventData, MajorityElectionUnion>();
    }
}
