// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class VoteTestProfile : Profile
{
    public VoteTestProfile()
    {
        CreateMap<Vote, Core.Domain.Vote>();
        CreateMap<Ballot, Core.Domain.Ballot>();
        CreateMap<BallotQuestion, Core.Domain.BallotQuestion>();
        CreateMap<TieBreakQuestion, Core.Domain.TieBreakQuestion>();

        CreateMap<ProtoModels.Vote, VoteEventData>();
        CreateMap<ProtoModels.Ballot, BallotEventData>();
        CreateMap<ProtoModels.BallotQuestion, BallotQuestionEventData>()
#pragma warning disable CS0612
            .ForMember(dst => dst.FederalIdentification, opt => opt.Ignore())
#pragma warning restore CS0612
            .ForMember(dst => dst.FederalIdentificationString, opt => opt.MapFrom(src => src.FederalIdentification));
        CreateMap<ProtoModels.TieBreakQuestion, TieBreakQuestionEventData>()
#pragma warning disable CS0612
            .ForMember(dst => dst.FederalIdentification, opt => opt.Ignore())
#pragma warning restore CS0612
            .ForMember(dst => dst.FederalIdentificationString, opt => opt.MapFrom(src => src.FederalIdentification));
    }
}
