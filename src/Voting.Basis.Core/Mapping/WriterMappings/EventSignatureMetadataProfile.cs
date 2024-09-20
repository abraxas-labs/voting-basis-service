// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Core.Domain;
using Voting.Basis.EventSignature.Models;
using ProtoEventSignatureBusinessMetadata = Abraxas.Voting.Basis.Events.V1.Metadata.EventSignatureBusinessMetadata;
using ProtoEventSignaturePublicKeyMetadata = Abraxas.Voting.Basis.Events.V1.Metadata.EventSignaturePublicKeyMetadata;

namespace Voting.Basis.Core.Mapping.WriterMappings;

public class EventSignatureMetadataProfile : Profile
{
    public EventSignatureMetadataProfile()
    {
        CreateMap<EventSignatureBusinessMetadata, ProtoEventSignatureBusinessMetadata>();
        CreateMap<EventSignaturePublicKeyCreate, ProtoEventSignaturePublicKeyMetadata>();
        CreateMap<EventSignaturePublicKeyDelete, ProtoEventSignaturePublicKeyMetadata>();
    }
}
