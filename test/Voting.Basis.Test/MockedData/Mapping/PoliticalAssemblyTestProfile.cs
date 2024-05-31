// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class PoliticalAssemblyTestProfile : Profile
{
    public PoliticalAssemblyTestProfile()
    {
        CreateMap<PoliticalAssembly, Core.Domain.PoliticalAssembly>();
        CreateMap<PoliticalAssemblyEventData, Core.Domain.PoliticalAssembly>();
        CreateMap<ProtoModels.PoliticalAssembly, PoliticalAssemblyEventData>();
    }
}
