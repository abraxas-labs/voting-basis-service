// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public sealed class DomainOfInfluenceProfile : Profile
{
    public DomainOfInfluenceProfile()
    {
        CreateMap<DomainOfInfluenceEventData, DomainOfInfluence>();
        CreateMap<DomainOfInfluencePartyEventData, DomainOfInfluenceParty>();
        CreateMap<DomainOfInfluenceVotingCardPrintDataEventData, Domain.DomainOfInfluenceVotingCardPrintData>();
        CreateMap<DomainOfInfluenceVotingCardPrintDataEventData, DomainOfInfluenceVotingCardPrintData>();
        CreateMap<DomainOfInfluenceVotingCardReturnAddressEventData, DomainOfInfluenceVotingCardReturnAddress>();
        CreateMap<DomainOfInfluenceVotingCardDataUpdated, DomainOfInfluence>();
        CreateMap<DomainOfInfluenceVotingCardSwissPostDataEventData, Domain.DomainOfInfluenceVotingCardSwissPostData>();
        CreateMap<DomainOfInfluenceVotingCardSwissPostDataEventData, DomainOfInfluenceVotingCardSwissPostData>();
    }
}
