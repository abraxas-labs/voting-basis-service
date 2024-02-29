// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        CreateMap<CountingCircleEventData, CountingCircle>();
        CreateMap<AuthorityEventData, Authority>();
        CreateMap<CountingCirclesMergerEventData, CountingCirclesMerger>();
        CreateMap<CountingCircleElectorateEventData, CountingCircleElectorate>();
    }
}
