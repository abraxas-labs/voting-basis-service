// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class ProportionalElectionUnionProfile : Profile
{
    public ProportionalElectionUnionProfile()
    {
        CreateMap<ProportionalElectionUnion, ProportionalElectionUnionEventData>();
        CreateMap<ProportionalElectionUnionEventData, ProportionalElectionUnionAggregate>();
    }
}
