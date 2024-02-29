// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        CreateMap<Domain.CountingCircle, CountingCircleEventData>().ReverseMap();
        CreateMap<CountingCircleEventData, CountingCircleAggregate>().ReverseMap();
        CreateMap<Domain.ContactPerson, ContactPersonEventData>().ReverseMap();
        CreateMap<Domain.Authority, AuthorityEventData>().ReverseMap();
        CreateMap<Domain.CountingCirclesMerger, CountingCirclesMergerEventData>().ReverseMap();
        CreateMap<Domain.CountingCircleElectorate, CountingCircleElectorateEventData>().ReverseMap();

        // to copy counting circles (used in mergers)
        CreateMap<CountingCircleAggregate, Domain.CountingCircle>();
        CreateMap<Domain.Authority, Domain.Authority>();
    }
}
