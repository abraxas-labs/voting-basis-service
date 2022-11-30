// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class PlausibilisationProfile : Profile
{
    public PlausibilisationProfile()
    {
        // write
        CreateMap<ProtoModels.PlausibilisationConfiguration, Core.Domain.PlausibilisationConfiguration>();
        CreateMap<ProtoModels.ComparisonVoterParticipationConfiguration, Core.Domain.ComparisonVoterParticipationConfiguration>();
        CreateMap<ProtoModels.ComparisonVotingChannelConfiguration, Core.Domain.ComparisonVotingChannelConfiguration>();
        CreateMap<ProtoModels.ComparisonCountOfVotersConfiguration, Core.Domain.ComparisonCountOfVotersConfiguration>();
        CreateMap<ProtoModels.ComparisonCountOfVotersCountingCircleEntry, Core.Domain.ComparisonCountOfVotersCountingCircleEntry>();

        // read
        CreateMap<Data.Models.PlausibilisationConfiguration, ProtoModels.PlausibilisationConfiguration>()
            .ForMember(
                dst =>
                    dst.ComparisonCountOfVotersCountingCircleEntries,
                opts =>
                    opts.MapFrom(src => src.DomainOfInfluence != null && src.DomainOfInfluence.CountingCircles != null
                        ? src.DomainOfInfluence.CountingCircles.Where(x => x.ComparisonCountOfVotersCategory != Data.Models.ComparisonCountOfVotersCategory.Unspecified).ToList()
                        : new List<Data.Models.DomainOfInfluenceCountingCircle>()));

        CreateMap<Data.Models.ComparisonVoterParticipationConfiguration, ProtoModels.ComparisonVoterParticipationConfiguration>();
        CreateMap<Data.Models.ComparisonVotingChannelConfiguration, ProtoModels.ComparisonVotingChannelConfiguration>();
        CreateMap<Data.Models.ComparisonCountOfVotersConfiguration, ProtoModels.ComparisonCountOfVotersConfiguration>();

        CreateMap<Data.Models.DomainOfInfluenceCountingCircle, ProtoModels.ComparisonCountOfVotersCountingCircleEntry>()
            .ForMember(dst => dst.Category, opts => opts.MapFrom(src => src.ComparisonCountOfVotersCategory));
    }
}
