// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class EntityOrderProfile : Profile
{
    public EntityOrderProfile()
    {
        // write
        CreateMap<ProtoModels.EntityOrder, Core.Domain.EntityOrder>();
    }
}
