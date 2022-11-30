// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class MajorityElectionUnionProfile : Profile
{
    public MajorityElectionUnionProfile()
    {
        // write
        CreateMap<CreateMajorityElectionUnionRequest, Core.Domain.MajorityElectionUnion>();
        CreateMap<UpdateMajorityElectionUnionRequest, Core.Domain.MajorityElectionUnion>();

        // read
        CreateMap<Data.Models.MajorityElectionUnion, ProtoModels.MajorityElectionUnion>();
    }
}
