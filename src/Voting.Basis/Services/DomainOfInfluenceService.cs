// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Read.Snapshot;
using Voting.Basis.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.DomainOfInfluenceService.DomainOfInfluenceServiceBase;

namespace Voting.Basis.Services;

public class DomainOfInfluenceService : ServiceBase
{
    private readonly DomainOfInfluenceReader _domainOfInfluenceReader;
    private readonly DomainOfInfluenceWriter _domainOfInfluenceWriter;
    private readonly DomainOfInfluenceSnapshotReader _domainOfInfluenceSnapshotReader;
    private readonly IMapper _mapper;

    public DomainOfInfluenceService(
        DomainOfInfluenceReader domainOfInfluenceReader,
        DomainOfInfluenceWriter domainOfInfluenceWriter,
        DomainOfInfluenceSnapshotReader domainOfInfluenceSnapshotReader,
        IMapper mapper)
    {
        _domainOfInfluenceReader = domainOfInfluenceReader;
        _domainOfInfluenceWriter = domainOfInfluenceWriter;
        _domainOfInfluenceSnapshotReader = domainOfInfluenceSnapshotReader;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.DomainOfInfluence.Create)]
    public override async Task<IdValue> Create(
        CreateDomainOfInfluenceRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.DomainOfInfluence>(request);
        await _domainOfInfluenceWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.DomainOfInfluence.Update)]
    public override async Task<Empty> Update(
        UpdateDomainOfInfluenceRequest request,
        ServerCallContext context)
    {
        switch (request.AdminOrElectionAdminRequestCase)
        {
            case UpdateDomainOfInfluenceRequest.AdminOrElectionAdminRequestOneofCase.AdminRequest:
                await _domainOfInfluenceWriter.UpdateForAdmin(_mapper.Map<Core.Domain.DomainOfInfluence>(request.AdminRequest));
                break;
            case UpdateDomainOfInfluenceRequest.AdminOrElectionAdminRequestOneofCase.ElectionAdminRequest:
                await _domainOfInfluenceWriter.UpdateForElectionAdmin(_mapper.Map<Core.Domain.DomainOfInfluence>(request.ElectionAdminRequest));
                break;
            default:
                throw new ValidationException($"either {nameof(UpdateDomainOfInfluenceRequest.AdminOrElectionAdminRequestOneofCase.AdminRequest)} or {nameof(UpdateDomainOfInfluenceRequest.AdminOrElectionAdminRequestOneofCase.ElectionAdminRequest)} must be set");
        }

        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.DomainOfInfluence.Delete)]
    public override async Task<Empty> Delete(DeleteDomainOfInfluenceRequest request, ServerCallContext context)
    {
        await _domainOfInfluenceWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.DomainOfInfluence.Read)]
    public override async Task<DomainOfInfluence> Get(
        GetDomainOfInfluenceRequest request,
        ServerCallContext context)
        => _mapper.Map<DomainOfInfluence>(await _domainOfInfluenceReader.Get(GuidParser.Parse(request.Id)));

    [AuthorizePermission(Permissions.DomainOfInfluence.Read)]
    public override async Task<DomainOfInfluences> List(
        ListDomainOfInfluenceRequest request,
        ServerCallContext context)
    {
        var domainOfInfluences = request.FilterCase switch
        {
            ListDomainOfInfluenceRequest.FilterOneofCase.CountingCircleId => await _domainOfInfluenceReader.ListForCountingCircle(GuidParser.Parse(request.CountingCircleId)),
            ListDomainOfInfluenceRequest.FilterOneofCase.SecureConnectId => await _domainOfInfluenceReader.ListForSecureConnectId(request.SecureConnectId),
            ListDomainOfInfluenceRequest.FilterOneofCase.ContestDomainOfInfluenceId => await _domainOfInfluenceReader.ListForPoliticalBusiness(GuidParser.Parse(request.ContestDomainOfInfluenceId)),
            _ => await _domainOfInfluenceReader.ListForSecureConnectId(string.Empty),
        };

        return _mapper.Map<DomainOfInfluences>(domainOfInfluences);
    }

    [AuthorizePermission(Permissions.DomainOfInfluenceHierarchy.Read)]
    public override async Task<DomainOfInfluences> ListTree(
        ListTreeDomainOfInfluenceRequest request,
        ServerCallContext context)
        => _mapper.Map<DomainOfInfluences>(await _domainOfInfluenceReader.ListTree());

    [AuthorizePermission(Permissions.DomainOfInfluenceHierarchy.Update)]
    public override async Task<Empty> UpdateCountingCircleEntries(
        UpdateDomainOfInfluenceCountingCircleEntriesRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.DomainOfInfluenceCountingCircleEntries>(request);
        await _domainOfInfluenceWriter.UpdateDomainOfInfluenceCountingCircles(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.DomainOfInfluenceHierarchy.Read)]
    public override async Task<DomainOfInfluences> ListTreeSnapshot(
        ListTreeDomainOfInfluenceSnapshotRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<DomainOfInfluences>(await _domainOfInfluenceSnapshotReader.ListTree(request.DateTime?.ToDateTime() ?? DateTime.UtcNow, request.IncludeDeleted));
    }

    [AuthorizePermission(Permissions.DomainOfInfluence.Read)]
    public override async Task<DomainOfInfluences> ListSnapshot(
        ListDomainOfInfluenceSnapshotRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<DomainOfInfluences>(await _domainOfInfluenceSnapshotReader.ListForCountingCircle(request.CountingCircleId, request.DateTime?.ToDateTime() ?? DateTime.UtcNow));
    }

    [AuthorizePermission(Permissions.DomainOfInfluence.Read)]
    public override async Task<DomainOfInfluenceCantonDefaults> GetCantonDefaults(
        GetDomainOfInfluenceCantonDefaultsRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<DomainOfInfluenceCantonDefaults>(await _domainOfInfluenceReader.GetCantonDefaults(GuidParser.Parse(request.DomainOfInfluenceId)));
    }

    [AuthorizePermission(Permissions.DomainOfInfluenceLogo.Read)]
    public override async Task<DomainOfInfluenceLogo> GetLogo(GetDomainOfInfluenceLogoRequest request, ServerCallContext context)
    {
        var doiId = GuidParser.Parse(request.DomainOfInfluenceId);
        var url = await _domainOfInfluenceReader.GetLogoUrl(doiId);
        return new DomainOfInfluenceLogo
        {
            LogoUrl = url.ToString(),
            DomainOfInfluenceId = doiId.ToString(),
        };
    }

    [AuthorizePermission(Permissions.DomainOfInfluenceLogo.Delete)]
    public override async Task<Empty> DeleteLogo(DeleteDomainOfInfluenceLogoRequest request, ServerCallContext context)
    {
        await _domainOfInfluenceWriter.DeleteLogo(GuidParser.Parse(request.DomainOfInfluenceId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.DomainOfInfluence.Read)]
    public override async Task<DomainOfInfluenceParties> ListParties(ListDomainOfInfluencePartiesRequest request, ServerCallContext context)
    {
        return _mapper.Map<DomainOfInfluenceParties>(await _domainOfInfluenceReader.ListParties(GuidParser.Parse(request.DomainOfInfluenceId)));
    }
}
