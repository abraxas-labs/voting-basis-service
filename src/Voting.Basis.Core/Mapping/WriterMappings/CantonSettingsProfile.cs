// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class CantonSettingsProfile : Profile
{
    public CantonSettingsProfile()
    {
        CreateMap<CantonSettings, CantonSettingsEventData>();
        CreateMap<CantonSettingsEventData, CantonSettingsAggregate>();
        CreateMap<CantonSettingsVotingCardChannel, CantonSettingsVotingCardChannelEventData>().ReverseMap();
    }
}
