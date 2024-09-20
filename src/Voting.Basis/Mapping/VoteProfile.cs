// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class VoteProfile : Profile
{
    public VoteProfile()
    {
        // write
        CreateMap<CreateVoteRequest, Core.Domain.Vote>();
        CreateMap<UpdateVoteRequest, Core.Domain.Vote>();
        CreateMap<CreateBallotRequest, Core.Domain.Ballot>();
        CreateMap<UpdateBallotRequest, Core.Domain.Ballot>();
        CreateMap<ProtoModels.BallotQuestion, Core.Domain.BallotQuestion>();
        CreateMap<ProtoModels.TieBreakQuestion, Core.Domain.TieBreakQuestion>();

        // read
        CreateMap<Data.Models.Vote, ProtoModels.Vote>();
        CreateMap<Data.Models.Ballot, ProtoModels.Ballot>();
        CreateMap<Data.Models.BallotQuestion, ProtoModels.BallotQuestion>();
        CreateMap<Data.Models.TieBreakQuestion, ProtoModels.TieBreakQuestion>();
    }
}
