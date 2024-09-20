// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public class PlausibilisationProfile : Profile
{
    public PlausibilisationProfile()
    {
        CreateMap<PlausibilisationConfigurationEventData, PlausibilisationConfiguration>();
        CreateMap<ComparisonVoterParticipationConfigurationEventData, ComparisonVoterParticipationConfiguration>();
        CreateMap<ComparisonVotingChannelConfigurationEventData, ComparisonVotingChannelConfiguration>();
        CreateMap<ComparisonCountOfVotersConfigurationEventData, ComparisonCountOfVotersConfiguration>();
    }
}
