// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class EventSignatureProfile : Profile
{
    public EventSignatureProfile()
    {
        CreateMap<EventSignaturePublicKeyCreate, EventSignaturePublicKeyCreated>();
        CreateMap<EventSignaturePublicKeyDelete, EventSignaturePublicKeyDeleted>();
    }
}
