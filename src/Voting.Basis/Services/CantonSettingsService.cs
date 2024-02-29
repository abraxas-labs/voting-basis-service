// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.CantonSettingsService.CantonSettingsServiceBase;

namespace Voting.Basis.Services;

public class CantonSettingsService : ServiceBase
{
    private readonly CantonSettingsReader _cantonSettingsReader;
    private readonly CantonSettingsWriter _cantonSettingsWriter;
    private readonly IMapper _mapper;

    public CantonSettingsService(
        CantonSettingsReader cantonSettingsReader,
        CantonSettingsWriter cantonSettingsWriter,
        IMapper mapper)
    {
        _cantonSettingsReader = cantonSettingsReader;
        _cantonSettingsWriter = cantonSettingsWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.CantonSettings.Create)]
    public override async Task<IdValue> Create(
        CreateCantonSettingsRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.CantonSettings>(request);
        await _cantonSettingsWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.CantonSettings.Update)]
    public override async Task<Empty> Update(
        UpdateCantonSettingsRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.CantonSettings>(request);
        await _cantonSettingsWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.CantonSettings.Read)]
    public override async Task<CantonSettingsList> List(
        ListCantonSettingsRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<CantonSettingsList>(await _cantonSettingsReader.List());
    }

    [AuthorizePermission(Permissions.CantonSettings.Read)]
    public override async Task<CantonSettings> Get(
        GetCantonSettingsRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<CantonSettings>(await _cantonSettingsReader.Get(GuidParser.Parse(request.Id)));
    }
}
