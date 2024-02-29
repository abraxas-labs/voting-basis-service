// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ProportionalElectionUnionProfile : Profile
{
    public ProportionalElectionUnionProfile()
    {
        // write
        CreateMap<CreateProportionalElectionUnionRequest, Core.Domain.ProportionalElectionUnion>();
        CreateMap<UpdateProportionalElectionUnionRequest, Core.Domain.ProportionalElectionUnion>();

        // read
        CreateMap<Data.Models.ProportionalElectionUnion, ProtoModels.ProportionalElectionUnion>();

        CreateMap<Data.Models.ProportionalElectionUnionList, ProtoModels.ProportionalElectionUnionList>();
        CreateMap<IEnumerable<Data.Models.ProportionalElectionUnionList>, ProtoModels.ProportionalElectionUnionLists>()
            .ForMember(dst => dst.ProportionalElectionUnionLists_, opts => opts.MapFrom(src => src));
    }
}
