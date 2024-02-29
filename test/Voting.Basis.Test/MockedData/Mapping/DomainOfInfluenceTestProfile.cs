// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class DomainOfInfluenceTestProfile : Profile
{
    public DomainOfInfluenceTestProfile()
    {
        CreateMap<DomainOfInfluence, Core.Domain.DomainOfInfluence>();
        CreateMap<ProtoModels.DomainOfInfluence, DomainOfInfluenceEventData>();
        CreateMap<DomainOfInfluenceVotingCardPrintData, Core.Domain.DomainOfInfluenceVotingCardPrintData>();
        CreateMap<DomainOfInfluenceVotingCardReturnAddress, Core.Domain.DomainOfInfluenceVotingCardReturnAddress>();
        CreateMap<DomainOfInfluenceVotingCardSwissPostData, Core.Domain.DomainOfInfluenceVotingCardSwissPostData>();
    }
}
