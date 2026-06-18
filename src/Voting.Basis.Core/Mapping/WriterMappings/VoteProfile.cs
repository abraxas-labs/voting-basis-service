// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class VoteProfile : Profile
{
    public VoteProfile()
    {
        CreateMap<Vote, VoteEventData>();
        CreateMap<VoteEventData, VoteAggregate>();
        CreateMap<Ballot, BallotEventData>().ReverseMap();
        CreateMap<BallotQuestion, BallotQuestionEventData>()
#pragma warning disable CS0612
            .ForMember(dst => dst.FederalIdentification, opt => opt.Ignore())
#pragma warning restore CS0612
            .ForMember(dst => dst.FederalIdentificationString, opt => opt.MapFrom(src => src.FederalIdentification))
            .ReverseMap();
        CreateMap<TieBreakQuestion, TieBreakQuestionEventData>()
#pragma warning disable CS0612
            .ForMember(dst => dst.FederalIdentification, opt => opt.Ignore())
#pragma warning restore CS0612
            .ForMember(dst => dst.FederalIdentificationString, opt => opt.MapFrom(src => src.FederalIdentification))
            .ReverseMap();
    }
}
