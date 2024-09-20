// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class ContestTestProfile : Profile
{
    public ContestTestProfile()
    {
        CreateMap<Contest, Core.Domain.Contest>();
        CreateMap<ContestEventData, Core.Domain.Contest>();
        CreateMap<ProtoModels.Contest, ContestEventData>();
    }
}
