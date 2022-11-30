// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ContactPersonProfile : Profile
{
    public ContactPersonProfile()
    {
        // write
        CreateMap<ProtoModels.ContactPerson, Core.Domain.ContactPerson>();

        // read
        CreateMap<Data.Models.ContactPerson, ProtoModels.ContactPerson>();
    }
}
