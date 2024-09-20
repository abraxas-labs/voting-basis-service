// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class ElectionGroupProfile : Profile
{
    public ElectionGroupProfile()
    {
        CreateMap<ElectionGroup, ElectionGroupEventData>().ReverseMap();
    }
}
