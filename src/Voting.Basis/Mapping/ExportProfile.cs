// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using Voting.Lib.VotingExports.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ExportProfile : Profile
{
    public ExportProfile()
    {
        // read
        CreateMap<IEnumerable<TemplateModel>, ProtoModels.ExportTemplates>()
            .ForMember(dst => dst.ExportTemplates_, opts => opts.MapFrom(src => src));

        CreateMap<TemplateModel, ProtoModels.ExportTemplate>();

        CreateMap<Data.Models.ExportConfiguration, ProtoModels.ExportConfiguration>();
    }
}
