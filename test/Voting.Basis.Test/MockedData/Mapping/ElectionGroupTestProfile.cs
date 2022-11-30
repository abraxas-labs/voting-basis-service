// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class ElectionGroupTestProfile : Profile
{
    public ElectionGroupTestProfile()
    {
        CreateMap<ElectionGroup, Core.Domain.ElectionGroup>();
    }
}
