// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Google.Protobuf.WellKnownTypes;

namespace Voting.Basis.Mapping;

public class BooleanProfile : Profile
{
    public BooleanProfile()
    {
        CreateMap<BoolValue, bool>()
            .ConvertUsing(src => src.Value);

        CreateMap<bool, BoolValue>()
            .ConvertUsing(src => new BoolValue { Value = src });
    }
}
