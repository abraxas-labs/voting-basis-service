// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Data.Models;
using MajorityElection = Voting.Basis.Data.Models.MajorityElection;
using MajorityElectionBallotGroup = Voting.Basis.Data.Models.MajorityElectionBallotGroup;
using MajorityElectionBallotGroupEntry = Voting.Basis.Data.Models.MajorityElectionBallotGroupEntry;
using MajorityElectionCandidate = Voting.Basis.Data.Models.MajorityElectionCandidate;
using MajorityElectionUnion = Voting.Basis.Data.Models.MajorityElectionUnion;
using SecondaryMajorityElection = Voting.Basis.Data.Models.SecondaryMajorityElection;

namespace Voting.Basis.Test.MockedData.Mapping;

public class MajorityElectionTestProfile : Profile
{
    public MajorityElectionTestProfile()
    {
        CreateMap<MajorityElection, Core.Domain.MajorityElection>();
        CreateMap<SecondaryMajorityElection, Core.Domain.SecondaryMajorityElection>();
        CreateMap<MajorityElectionCandidate, Core.Domain.MajorityElectionCandidate>();
        CreateMap<SecondaryMajorityElectionCandidate, Core.Domain.MajorityElectionCandidate>();
        CreateMap<MajorityElectionBallotGroup, Core.Domain.MajorityElectionBallotGroup>();
        CreateMap<MajorityElectionBallotGroupEntry, Core.Domain.MajorityElectionBallotGroupEntry>()
            .ForMember(
                dst => dst.ElectionId,
                opts => opts.MapFrom(src => src.PrimaryMajorityElectionId ?? src.SecondaryMajorityElectionId));
        CreateMap<MajorityElectionBallotGroupEntryCandidate, MajorityElectionBallotGroupEntryCandidates>();
        CreateMap<MajorityElectionUnion, Core.Domain.MajorityElectionUnion>();
    }
}
