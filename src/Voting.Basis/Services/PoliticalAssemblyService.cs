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
using PoliticalAssembly = Abraxas.Voting.Basis.Services.V1.Models.PoliticalAssembly;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.PoliticalAssemblyService.PoliticalAssemblyServiceBase;

namespace Voting.Basis.Services;

public class PoliticalAssemblyService : ServiceBase
{
    private readonly PoliticalAssemblyReader _politicalAssemblyReader;
    private readonly PoliticalAssemblyWriter _politicalAssemblyWriter;
    private readonly IMapper _mapper;

    public PoliticalAssemblyService(
        PoliticalAssemblyReader politicalAssemblyReader,
        PoliticalAssemblyWriter politicalAssemblyWriter,
        IMapper mapper)
    {
        _politicalAssemblyReader = politicalAssemblyReader;
        _politicalAssemblyWriter = politicalAssemblyWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.PoliticalAssembly.Create)]
    public override async Task<IdValue> Create(
        CreatePoliticalAssemblyRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.PoliticalAssembly>(request);
        await _politicalAssemblyWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.PoliticalAssembly.Update)]
    public override async Task<Empty> Update(
        UpdatePoliticalAssemblyRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.PoliticalAssembly>(request);
        await _politicalAssemblyWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.PoliticalAssembly.Delete)]
    public override async Task<Empty> Delete(DeletePoliticalAssemblyRequest request, ServerCallContext context)
    {
        await _politicalAssemblyWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizeAnyPermission(Permissions.PoliticalAssembly.ReadTenantHierarchy, Permissions.PoliticalAssembly.ReadSameCanton, Permissions.PoliticalAssembly.ReadAll)]
    public override async Task<PoliticalAssembly> Get(
        GetPoliticalAssemblyRequest request,
        ServerCallContext context)
        => _mapper.Map<PoliticalAssembly>(await _politicalAssemblyReader.Get(GuidParser.Parse(request.Id)));

    [AuthorizeAnyPermission(Permissions.PoliticalAssembly.ReadTenantHierarchy, Permissions.PoliticalAssembly.ReadSameCanton, Permissions.PoliticalAssembly.ReadAll)]
    public override async Task<PoliticalAssemblies> List(ListPoliticalAssemblyRequest request, ServerCallContext context)
    {
        return _mapper.Map<PoliticalAssemblies>(await _politicalAssemblyReader.List());
    }
}
