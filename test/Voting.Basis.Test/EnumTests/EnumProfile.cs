// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using Voting.Basis.Test.MockedData;

namespace Voting.Basis.Test.EnumTests;

public class EnumProfile : Profile
{
    public EnumProfile()
    {
        CreateMap<EnumMockedData.TestEnum1, EnumMockedData.TestEnum2>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();
    }
}
