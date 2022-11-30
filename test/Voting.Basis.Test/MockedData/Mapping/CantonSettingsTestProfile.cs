// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class CantonSettingsTestProfile : Profile
{
    public CantonSettingsTestProfile()
    {
        CreateMap<CantonSettings, Core.Domain.CantonSettings>();
        CreateMap<CantonSettingsVotingCardChannel, Core.Domain.CantonSettingsVotingCardChannel>();
    }
}
