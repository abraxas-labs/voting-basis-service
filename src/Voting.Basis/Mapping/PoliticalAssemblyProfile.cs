// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Voting.Basis.Core.Domain;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public sealed class PoliticalAssemblyProfile : Profile
{
    public PoliticalAssemblyProfile()
    {
        // write
        CreateMap<CreatePoliticalAssemblyRequest, PoliticalAssembly>();
        CreateMap<UpdatePoliticalAssemblyRequest, PoliticalAssembly>();

        // read
        CreateMap<Data.Models.PoliticalAssembly, ProtoModels.PoliticalAssembly>();
        CreateMap<IEnumerable<Data.Models.PoliticalAssembly>, ProtoModels.PoliticalAssemblies>()
            .ForMember(dst => dst.PoliticalAssemblies_, opts => opts.MapFrom(src => src));
    }
}
