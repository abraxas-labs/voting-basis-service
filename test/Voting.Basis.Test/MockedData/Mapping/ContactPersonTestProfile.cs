// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class ContactPersonTestProfile : Profile
{
    public ContactPersonTestProfile()
    {
        CreateMap<ContactPerson, Core.Domain.ContactPerson>();
    }
}
