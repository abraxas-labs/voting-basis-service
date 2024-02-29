// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Voting.Basis.Core.Domain;
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

        // read
        CreateMap<Data.Models.CantonSettings, ProtoModels.CantonSettings>();
        CreateMap<Data.Models.CantonSettingsVotingCardChannel, ProtoModels.CantonSettingsVotingCardChannel>();
        CreateMap<IEnumerable<Data.Models.CantonSettings>, ProtoModels.CantonSettingsList>()
            .ForMember(dst => dst.CantonSettingsList_, opts => opts.MapFrom(src => src));
    }
}
