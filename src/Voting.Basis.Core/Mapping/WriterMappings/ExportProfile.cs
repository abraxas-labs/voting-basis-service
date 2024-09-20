// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class ExportProfile : Profile
{
    public ExportProfile()
    {
        CreateMap<Domain.ExportConfiguration, ExportConfigurationEventData>();
    }
}
