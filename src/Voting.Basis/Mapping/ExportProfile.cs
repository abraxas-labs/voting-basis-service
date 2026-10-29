// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using Voting.Basis.Data.Models;
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

        CreateMap<ExportConfiguration, ProtoModels.ExportConfiguration>()
            .ForMember(dst => dst.SedexConfiguration, opts =>
            {
                opts.PreCondition(x => x.SedexSenderId != null && x.SedexRecipientIds != null);
                opts.MapFrom(src => src);
            });
        CreateMap<ExportConfiguration, ProtoModels.ExportSedexConfiguration>()
            .ForMember(dst => dst.SenderId, opts => opts.MapFrom(src => src.SedexSenderId))
            .ForMember(dst => dst.RecipientIds, opts => opts.MapFrom(src => src.SedexRecipientIds));
    }
}
