// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Lib.Common;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ExportConfigurationProfile : Profile
{
    public ExportConfigurationProfile()
    {
        CreateMap<ProtoModels.ExportConfiguration, ExportConfiguration>()
        .ForMember(dst => dst.Id, opts => opts.MapFrom(x => GuidParser.ParseNullable(x.Id) ?? Guid.NewGuid()))
        .ReverseMap();
    }
}
