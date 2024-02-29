// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Read.Snapshot;
using Voting.Basis.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.CountingCircleService.CountingCircleServiceBase;

namespace Voting.Basis.Services;

public class CountingCircleService : ServiceBase
{
    private readonly CountingCircleWriter _countingCircleWriter;
    private readonly CountingCircleReader _countingCircleReader;
    private readonly CountingCircleSnapshotReader _countingCircleSnapshotReader;
    private readonly IMapper _mapper;

    public CountingCircleService(
        IMapper mapper,
        CountingCircleWriter countingCircleWriter,
        CountingCircleReader countingCircleReader,
        CountingCircleSnapshotReader countingCircleSnapshotReader)
    {
        _mapper = mapper;
        _countingCircleWriter = countingCircleWriter;
        _countingCircleReader = countingCircleReader;
        _countingCircleSnapshotReader = countingCircleSnapshotReader;
    }

    [AuthorizePermission(Permissions.CountingCircle.Create)]
    public override async Task<IdValue> Create(
        CreateCountingCircleRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.CountingCircle>(request);
        await _countingCircleWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.CountingCircle.Update)]
    public override async Task<Empty> Update(
        UpdateCountingCircleRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.CountingCircle>(request);
        await _countingCircleWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.CountingCircle.Delete)]
    public override async Task<Empty> Delete(DeleteCountingCircleRequest request, ServerCallContext context)
    {
        await _countingCircleWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.CountingCircle.Read)]
    public override async Task<CountingCircle> Get(GetCountingCircleRequest request, ServerCallContext context)
        => _mapper.Map<CountingCircle>(await _countingCircleReader.Get(GuidParser.Parse(request.Id)));

    [AuthorizePermission(Permissions.CountingCircle.Read)]
    public override async Task<CountingCircles> List(ListCountingCircleRequest request, ServerCallContext context)
    {
        return _mapper.Map<CountingCircles>(await _countingCircleReader.List());
    }

    [AuthorizePermission(Permissions.CountingCircle.Read)]
    public override async Task<DomainOfInfluenceCountingCircles> ListAssigned(ListAssignedCountingCircleRequest request, ServerCallContext context)
    {
        return _mapper.Map<DomainOfInfluenceCountingCircles>(await _countingCircleReader.ListForDomainOfInfluence(GuidParser.Parse(request.DomainOfInfluenceId)));
    }

    [AuthorizePermission(Permissions.CountingCircle.ReadAll)]
    public override async Task<CountingCircles> ListAssignable(ListAssignableCountingCircleRequest request, ServerCallContext context)
    {
        return _mapper.Map<CountingCircles>(await _countingCircleReader.GetAssignableListForDomainOfInfluence(GuidParser.Parse(request.DomainOfInfluenceId)));
    }

    [AuthorizePermission(Permissions.CountingCircle.Read)]
    public override async Task<CountingCircles> ListSnapshot(ListCountingCircleSnapshotRequest request, ServerCallContext context)
    {
        return _mapper.Map<CountingCircles>(await _countingCircleSnapshotReader.List(request.DateTime?.ToDateTime() ?? DateTime.UtcNow, request.IncludeDeleted));
    }

    [AuthorizePermission(Permissions.CountingCircle.Read)]
    public override async Task<DomainOfInfluenceCountingCircles> ListAssignedSnapshot(ListAssignedCountingCircleSnapshotRequest request, ServerCallContext context)
    {
        return _mapper.Map<DomainOfInfluenceCountingCircles>(await _countingCircleSnapshotReader.ListForDomainOfInfluence(request.DomainOfInfluenceId, request.DateTime?.ToDateTime() ?? DateTime.UtcNow));
    }

    [AuthorizePermission(Permissions.CountingCircle.Merge)]
    public override async Task<IdValue> ScheduleMerger(ScheduleCountingCirclesMergerRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.CountingCirclesMerger>(request);
        var id = await _countingCircleWriter.ScheduleMerge(data);
        return new IdValue { Id = id.ToString() };
    }

    [AuthorizePermission(Permissions.CountingCircle.Merge)]
    public override async Task<Empty> UpdateScheduledMerger(UpdateScheduledCountingCirclesMergerRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.CountingCirclesMerger>(request);
        await _countingCircleWriter.UpdateScheduledMerger(GuidParser.Parse(request.NewCountingCircleId), data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.CountingCircle.Merge)]
    public override async Task<Empty> DeleteScheduledMerger(DeleteScheduledCountingCirclesMergerRequest request, ServerCallContext context)
    {
        var newCcId = GuidParser.Parse(request.NewCountingCircleId);
        await _countingCircleWriter.DeleteScheduledMerger(newCcId);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.CountingCircle.Merge)]
    public override async Task<CountingCirclesMergers> ListMergers(ListCountingCirclesMergersRequest request, ServerCallContext context)
    {
        var data = await _countingCircleReader.ListMergers(request.Merged);
        return _mapper.Map<CountingCirclesMergers>(data);
    }
}
