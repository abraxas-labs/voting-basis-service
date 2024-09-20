// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class ExportConfigurationProfile : Profile
{
    public ExportConfigurationProfile()
    {
        CreateMap<Domain.ExportConfiguration, ExportConfigurationEventData>().ReverseMap();
    }
}
