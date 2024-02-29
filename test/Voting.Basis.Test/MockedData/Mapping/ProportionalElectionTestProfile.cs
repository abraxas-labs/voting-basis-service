// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class ProportionalElectionTestProfile : Profile
{
    public ProportionalElectionTestProfile()
    {
        CreateMap<ProportionalElection, Core.Domain.ProportionalElection>();
        CreateMap<ProportionalElectionList, Core.Domain.ProportionalElectionList>();
        CreateMap<ProportionalElectionCandidate, Core.Domain.ProportionalElectionCandidate>();
        CreateMap<ProportionalElectionListUnion, Core.Domain.ProportionalElectionListUnion>();
        CreateMap<ProportionalElectionUnion, Core.Domain.ProportionalElectionUnion>();
    }
}
