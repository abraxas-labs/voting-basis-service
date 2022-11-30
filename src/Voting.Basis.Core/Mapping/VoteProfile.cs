// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public class VoteProfile : Profile
{
    public VoteProfile()
    {
        CreateMap<VoteEventData, Vote>();
        CreateMap<VoteAfterTestingPhaseUpdated, Vote>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<VoteAfterTestingPhaseUpdated, VoteAggregate>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<BallotEventData, Ballot>()
            .AfterMap((_, dst) =>
            {
                foreach (var q in dst.BallotQuestions)
                {
                    q.BallotId = dst.Id;
                }

                foreach (var q in dst.TieBreakQuestions)
                {
                    q.BallotId = dst.Id;
                }
            });
        CreateMap<BallotAfterTestingPhaseUpdated, Domain.Ballot>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
        CreateMap<BallotAfterTestingPhaseUpdated, Ballot>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate())
            .AfterMap((_, dst) =>
            {
                foreach (var q in dst.BallotQuestions)
                {
                    q.BallotId = dst.Id;
                }

                foreach (var q in dst.TieBreakQuestions)
                {
                    q.BallotId = dst.Id;
                }
            });

        CreateMap<BallotQuestionEventData, BallotQuestion>();
        CreateMap<TieBreakQuestionEventData, TieBreakQuestion>();
    }
}
