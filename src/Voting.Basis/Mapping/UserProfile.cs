// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, ProtoModels.User>();
    }
}
