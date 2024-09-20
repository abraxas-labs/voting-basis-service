// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class PlausibilisationProfile : Profile
{
    public PlausibilisationProfile()
    {
        CreateMap<Domain.PlausibilisationConfiguration, PlausibilisationConfigurationEventData>().ReverseMap();
        CreateMap<Domain.ComparisonVoterParticipationConfiguration, ComparisonVoterParticipationConfigurationEventData>().ReverseMap();
        CreateMap<Domain.ComparisonVotingChannelConfiguration, ComparisonVotingChannelConfigurationEventData>().ReverseMap();
        CreateMap<Domain.ComparisonCountOfVotersConfiguration, ComparisonCountOfVotersConfigurationEventData>().ReverseMap();
        CreateMap<Domain.ComparisonCountOfVotersCountingCircleEntry, ComparisonCountOfVotersCountingCircleEntryEventData>().ReverseMap();
    }
}
