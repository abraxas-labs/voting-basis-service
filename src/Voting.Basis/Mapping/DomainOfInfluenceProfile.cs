// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class DomainOfInfluenceProfile : Profile
{
    public DomainOfInfluenceProfile()
    {
        // write
        CreateMap<CreateDomainOfInfluenceRequest, Core.Domain.DomainOfInfluence>();
        CreateMap<UpdateDomainOfInfluenceForAdminRequest, Core.Domain.DomainOfInfluence>();
        CreateMap<UpdateDomainOfInfluenceForElectionAdminRequest, Core.Domain.DomainOfInfluence>();
        CreateMap<UpdateDomainOfInfluenceCountingCircleEntriesRequest, Core.Domain.DomainOfInfluenceCountingCircleEntries>();
        CreateMap<ProtoModels.DomainOfInfluenceVotingCardReturnAddress, Core.Domain.DomainOfInfluenceVotingCardReturnAddress>();
        CreateMap<ProtoModels.DomainOfInfluenceVotingCardPrintData, Core.Domain.DomainOfInfluenceVotingCardPrintData>();
        CreateMap<ProtoModels.DomainOfInfluenceParty, Core.Domain.DomainOfInfluenceParty>();

        // read
        CreateMap<IEnumerable<Data.Models.DomainOfInfluence>, ProtoModels.DomainOfInfluences>()
            .ForMember(dst => dst.DomainOfInfluences_, opts => opts.MapFrom(src => src));

        CreateMap<ProtoModels.DomainOfInfluence, Data.Models.DomainOfInfluence>()
            .ForMember(dst => dst.CountingCircles, opts => opts.Ignore())
            .ForMember(dst => dst.Children, opts => opts.Ignore());
        CreateMap<Data.Models.DomainOfInfluence, ProtoModels.DomainOfInfluence>()
            .ForMember(dst => dst.Info, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.DomainOfInfluenceCantonDefaults, ProtoModels.DomainOfInfluenceCantonDefaults>();
        CreateMap<Data.Models.DomainOfInfluenceVotingCardPrintData, ProtoModels.DomainOfInfluenceVotingCardPrintData>();
        CreateMap<Data.Models.DomainOfInfluenceVotingCardReturnAddress, ProtoModels.DomainOfInfluenceVotingCardReturnAddress>();
    }
}
