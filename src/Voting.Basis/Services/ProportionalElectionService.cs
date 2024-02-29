// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ProportionalElectionService.ProportionalElectionServiceBase;

namespace Voting.Basis.Services;

public class ProportionalElectionService : ServiceBase
{
    private readonly ProportionalElectionReader _proportionalElectionReader;
    private readonly ProportionalElectionWriter _proportionalElectionWriter;
    private readonly IMapper _mapper;

    public ProportionalElectionService(
        ProportionalElectionReader proportionalElectionReader,
        ProportionalElectionWriter proportionalElectionWriter,
        IMapper mapper)
    {
        _proportionalElectionReader = proportionalElectionReader;
        _proportionalElectionWriter = proportionalElectionWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.ProportionalElection.Create)]
    public override async Task<ProtoModels.IdValue> Create(
        CreateProportionalElectionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElection>(request);
        await _proportionalElectionWriter.Create(data);
        return new ProtoModels.IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.ProportionalElection.Update)]
    public override async Task<Empty> Update(
        UpdateProportionalElectionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElection>(request);
        await _proportionalElectionWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElection.Update)]
    public override async Task<Empty> UpdateActiveState(
        UpdateProportionalElectionActiveStateRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.UpdateActiveState(GuidParser.Parse(request.Id), request.Active);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElection.Delete)]
    public override async Task<Empty> Delete(
        DeleteProportionalElectionRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElection.Read)]
    public override async Task<ProtoModels.ProportionalElection> Get(
        GetProportionalElectionRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<ProtoModels.ProportionalElection>(await _proportionalElectionReader.Get(GuidParser.Parse(request.Id)));
    }

    [AuthorizePermission(Permissions.ProportionalElection.Read)]
    public override async Task<ProtoModels.ProportionalElections> List(
        ListProportionalElectionRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<ProtoModels.ProportionalElections>(await _proportionalElectionReader.ListOwnedByTenantForContest(GuidParser.Parse(request.ContestId)));
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Create)]
    public override async Task<ProtoModels.IdValue> CreateList(
        CreateProportionalElectionListRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElectionList>(request);
        await _proportionalElectionWriter.CreateList(data);
        return new ProtoModels.IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Read)]
    public override async Task<ProtoModels.ProportionalElectionLists> GetLists(
        GetProportionalElectionListsRequest request,
        ServerCallContext context)
    {
        var lists = await _proportionalElectionReader.GetLists(GuidParser.Parse(request.ProportionalElectionId));
        return _mapper.Map<ProtoModels.ProportionalElectionLists>(lists);
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Read)]
    public override async Task<ProtoModels.ProportionalElectionList> GetList(
        GetProportionalElectionListRequest request,
        ServerCallContext context)
    {
        var list = await _proportionalElectionReader.GetList(GuidParser.Parse(request.Id));
        return _mapper.Map<ProtoModels.ProportionalElectionList>(list);
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Update)]
    public override async Task<Empty> UpdateList(
        UpdateProportionalElectionListRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElectionList>(request);
        await _proportionalElectionWriter.UpdateList(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Update)]
    public override async Task<Empty> ReorderLists(
        ReorderProportionalElectionListsRequest request,
        ServerCallContext context)
    {
        var entityOrders = request.Orders;
        await _proportionalElectionWriter.ReorderLists(GuidParser.Parse(request.ProportionalElectionId), _mapper.Map<List<EntityOrder>>(entityOrders.Orders));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionList.Delete)]
    public override async Task<Empty> DeleteList(
        DeleteProportionalElectionListRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.DeleteList(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Create)]
    public override async Task<ProtoModels.IdValue> CreateListUnion(
        CreateProportionalElectionListUnionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElectionListUnion>(request);
        await _proportionalElectionWriter.CreateListUnion(data);
        return new ProtoModels.IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Update)]
    public override async Task<Empty> UpdateListUnion(
        UpdateProportionalElectionListUnionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElectionListUnion>(request);
        await _proportionalElectionWriter.UpdateListUnion(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Update)]
    public override async Task<Empty> UpdateListUnionEntries(
        UpdateProportionalElectionListUnionEntriesRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElectionListUnionEntries>(request);
        await _proportionalElectionWriter.UpdateListUnionEntries(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Update)]
    public override async Task<Empty> UpdateListUnionMainList(
        UpdateProportionalElectionListUnionMainListRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.UpdateListUnionMainList(
            GuidParser.Parse(request.ProportionalElectionListUnionId),
            GuidParser.ParseNullable(request.ProportionalElectionMainListId));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Read)]
    public override async Task<ProtoModels.ProportionalElectionListUnions> GetListUnions(
        GetProportionalElectionListUnionsRequest request,
        ServerCallContext context)
    {
        var listUnions = await _proportionalElectionReader.GetListUnions(GuidParser.Parse(request.ProportionalElectionId));
        return _mapper.Map<ProtoModels.ProportionalElectionListUnions>(listUnions);
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Read)]
    public override async Task<ProtoModels.ProportionalElectionListUnion> GetListUnion(
        GetProportionalElectionListUnionRequest request,
        ServerCallContext context)
    {
        var list = await _proportionalElectionReader.GetListUnion(GuidParser.Parse(request.Id));
        return _mapper.Map<ProtoModels.ProportionalElectionListUnion>(list);
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Delete)]
    public override async Task<Empty> DeleteListUnion(
        DeleteProportionalElectionListUnionRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.DeleteListUnion(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionListUnion.Read)]
    public override async Task<Empty> ReorderListUnions(
        ReorderProportionalElectionListUnionsRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.ReorderListUnions(
            GuidParser.Parse(request.ProportionalElectionId),
            GuidParser.ParseNullable(request.ProportionalElectionRootListUnionId),
            _mapper.Map<List<EntityOrder>>(request.Orders?.Orders ?? Enumerable.Empty<ProtoModels.EntityOrder>()));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionCandidate.Create)]
    public override async Task<ProtoModels.IdValue> CreateCandidate(
        CreateProportionalElectionCandidateRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElectionCandidate>(request);
        await _proportionalElectionWriter.CreateCandidate(data);
        return new ProtoModels.IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.ProportionalElectionCandidate.Read)]
    public override async Task<ProtoModels.ProportionalElectionCandidates> GetCandidates(
        GetProportionalElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var candidates = await _proportionalElectionReader.GetCandidates(GuidParser.Parse(request.ProportionalElectionListId));
        return _mapper.Map<ProtoModels.ProportionalElectionCandidates>(candidates);
    }

    [AuthorizePermission(Permissions.ProportionalElectionCandidate.Read)]
    public override async Task<ProtoModels.ProportionalElectionCandidate> GetCandidate(
        GetProportionalElectionCandidateRequest request,
        ServerCallContext context)
    {
        var candidate = await _proportionalElectionReader.GetCandidate(GuidParser.Parse(request.Id));
        return _mapper.Map<ProtoModels.ProportionalElectionCandidate>(candidate);
    }

    [AuthorizePermission(Permissions.ProportionalElectionCandidate.Update)]
    public override async Task<Empty> UpdateCandidate(
        UpdateProportionalElectionCandidateRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<ProportionalElectionCandidate>(request);
        await _proportionalElectionWriter.UpdateCandidate(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionCandidate.Update)]
    public override async Task<Empty> ReorderCandidates(
        ReorderProportionalElectionCandidatesRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.ReorderCandidates(
            GuidParser.Parse(request.ProportionalElectionListId),
            _mapper.Map<List<EntityOrder>>(request.Orders.Orders));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionCandidate.Delete)]
    public override async Task<Empty> DeleteCandidate(
        DeleteProportionalElectionCandidateRequest request,
        ServerCallContext context)
    {
        await _proportionalElectionWriter.DeleteCandidate(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }
}
