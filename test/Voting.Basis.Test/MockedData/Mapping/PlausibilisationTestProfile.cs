// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class PlausibilisationTestProfile : Profile
{
    public PlausibilisationTestProfile()
    {
        CreateMap<ProtoModels.PlausibilisationConfiguration, PlausibilisationConfigurationEventData>();
        CreateMap<ProtoModels.ComparisonVoterParticipationConfiguration, ComparisonVoterParticipationConfigurationEventData>();
        CreateMap<ProtoModels.ComparisonVotingChannelConfiguration, ComparisonVotingChannelConfigurationEventData>();
        CreateMap<ProtoModels.ComparisonCountOfVotersConfiguration, ComparisonCountOfVotersConfigurationEventData>();
        CreateMap<ProtoModels.ComparisonCountOfVotersCountingCircleEntry, ComparisonCountOfVotersCountingCircleEntryEventData>();

        CreateMap<PlausibilisationConfiguration, Core.Domain.PlausibilisationConfiguration>();

        CreateMap<ComparisonVoterParticipationConfiguration, Core.Domain.ComparisonVoterParticipationConfiguration>();
        CreateMap<ComparisonVotingChannelConfiguration, Core.Domain.ComparisonVotingChannelConfiguration>();
        CreateMap<ComparisonCountOfVotersConfiguration, Core.Domain.ComparisonCountOfVotersConfiguration>();
    }
}
