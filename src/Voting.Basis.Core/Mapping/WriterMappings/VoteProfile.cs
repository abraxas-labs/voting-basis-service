// (c) Copyright 2022 by Abraxas Informatik AG
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
        CreateMap<BallotQuestion, BallotQuestionEventData>().ReverseMap();
        CreateMap<TieBreakQuestion, TieBreakQuestionEventData>().ReverseMap();
    }
}
