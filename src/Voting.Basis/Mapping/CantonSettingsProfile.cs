// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using CantonSettings = Voting.Basis.Core.Domain.CantonSettings;
using CantonSettingsVotingCardChannel = Voting.Basis.Core.Domain.CantonSettingsVotingCardChannel;
using CountingCircleResultStateDescription = Voting.Basis.Core.Domain.CountingCircleResultStateDescription;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class CantonSettingsProfile : Profile
{
    public CantonSettingsProfile()
    {
        // write
        CreateMap<CreateCantonSettingsRequest, CantonSettings>();
        CreateMap<UpdateCantonSettingsRequest, CantonSettings>();
        CreateMap<ProtoModels.CantonSettingsVotingCardChannel, CantonSettingsVotingCardChannel>();
        CreateMap<ProtoModels.CountingCircleResultStateDescription, CountingCircleResultStateDescription>();

        // read
        CreateMap<Data.Models.CantonSettings, ProtoModels.CantonSettings>();
        CreateMap<Data.Models.CantonSettingsVotingCardChannel, ProtoModels.CantonSettingsVotingCardChannel>();
        CreateMap<Data.Models.CountingCircleResultStateDescription, ProtoModels.CountingCircleResultStateDescription>();
        CreateMap<IEnumerable<Data.Models.CantonSettings>, ProtoModels.CantonSettingsList>()
            .ForMember(dst => dst.CantonSettingsList_, opts => opts.MapFrom(src => src));
    }
}
