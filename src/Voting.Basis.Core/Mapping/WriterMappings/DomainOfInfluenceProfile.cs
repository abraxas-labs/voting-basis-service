// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class DomainOfInfluenceProfile : Profile
{
    public DomainOfInfluenceProfile()
    {
        CreateMap<Domain.DomainOfInfluence, DomainOfInfluenceEventData>();
        CreateMap<DomainOfInfluenceEventData, DomainOfInfluenceAggregate>();
        CreateMap<DomainOfInfluenceVotingCardDataUpdated, DomainOfInfluenceAggregate>();
        CreateMap<Domain.DomainOfInfluenceCountingCircleEntries, DomainOfInfluenceCountingCircleEntriesEventData>();
        CreateMap<Domain.DomainOfInfluenceVotingCardReturnAddress, DomainOfInfluenceVotingCardReturnAddressEventData>().ReverseMap();
        CreateMap<Domain.DomainOfInfluenceVotingCardPrintData, DomainOfInfluenceVotingCardPrintDataEventData>().ReverseMap();
        CreateMap<Domain.DomainOfInfluenceVotingCardSwissPostData, DomainOfInfluenceVotingCardSwissPostDataEventData>().ReverseMap();
        CreateMap<Domain.DomainOfInfluenceParty, DomainOfInfluencePartyEventData>().ReverseMap();
    }
}
