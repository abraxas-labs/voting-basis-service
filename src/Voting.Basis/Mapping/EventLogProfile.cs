// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using AutoMapper;
using Voting.Lib.Database.Models;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class EventLogProfile : Profile
{
    public EventLogProfile()
    {
        // read
        CreateMap<Data.Models.EventLog, ProtoModels.EventLog>();
        CreateMap<Data.Models.EventLogUser, ProtoModels.EventLogUser>();
        CreateMap<Data.Models.EventLogTenant, ProtoModels.EventLogTenant>();

        CreateMap<IEnumerable<Data.Models.EventLog>, ProtoModels.EventLogs>()
            .ForMember(dst => dst.EventLogs_, opts => opts.MapFrom(src => src));

        CreateMap<Page<Data.Models.EventLog>, ProtoModels.EventLogsPage>()
            .ForMember(dst => dst.Page, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.EventLogs, opts => opts.MapFrom(src => src.Items));
    }
}
