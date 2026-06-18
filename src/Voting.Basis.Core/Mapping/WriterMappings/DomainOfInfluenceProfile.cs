// (c) Copyright by Abraxas Informatik AG
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
        CreateMap<DomainOfInfluenceEventData, DomainOfInfluenceAggregate>()
            .ForMember(
                dst => dst.StistatExportEaiMessageType,
                opt => opt.Condition(src => src.StistatExportEaiMessageTypeSupported));
        CreateMap<DomainOfInfluenceVotingCardDataUpdated, DomainOfInfluenceAggregate>()
            .ForMember(
                dst => dst.StistatExportEaiMessageType,
                opt => opt.Condition(src => !src.StistatExportEaiMessageTypeDeprecated));
        CreateMap<Domain.DomainOfInfluenceCountingCircleEntries, DomainOfInfluenceCountingCircleEntriesEventData>();
        CreateMap<Domain.DomainOfInfluenceVotingCardReturnAddress, DomainOfInfluenceVotingCardReturnAddressEventData>().ReverseMap();
        CreateMap<Domain.DomainOfInfluenceVotingCardPrintData, DomainOfInfluenceVotingCardPrintDataEventData>().ReverseMap();
        CreateMap<Domain.DomainOfInfluenceVotingCardSwissPostData, DomainOfInfluenceVotingCardSwissPostDataEventData>().ReverseMap();
        CreateMap<Domain.DomainOfInfluenceParty, DomainOfInfluencePartyEventData>().ReverseMap();
    }
}
