// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ProportionalElectionUnionService.ProportionalElectionUnionServiceBase;

namespace Voting.Basis.Services;

public class ProportionalElectionUnionService : ServiceBase
{
    private readonly ProportionalElectionUnionReader _proportionalElectionUnionReader;
    private readonly ProportionalElectionUnionWriter _proportionalElectionUnionWriter;
    private readonly ProportionalElectionWriter _proportionalElectionWriter;
    private readonly IMapper _mapper;

    public ProportionalElectionUnionService(
        ProportionalElectionUnionReader proportionalElectionUnionReader,
        ProportionalElectionUnionWriter proportionalElectionUnionWriter,
        ProportionalElectionWriter proportionalElectionWriter,
        IMapper mapper)
    {
        _proportionalElectionUnionReader = proportionalElectionUnionReader;
        _proportionalElectionUnionWriter = proportionalElectionUnionWriter;
        _proportionalElectionWriter = proportionalElectionWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Create)]
    public override async Task<IdValue> Create(CreateProportionalElectionUnionRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.ProportionalElectionUnion>(request);
        await _proportionalElectionUnionWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Update)]
    public override async Task<Empty> Update(UpdateProportionalElectionUnionRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.ProportionalElectionUnion>(request);
        await _proportionalElectionUnionWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Update)]
    public override async Task<Empty> UpdateEntries(UpdateProportionalElectionUnionEntriesRequest request, ServerCallContext context)
    {
        await _proportionalElectionUnionWriter.UpdateEntries(
            GuidParser.Parse(request.ProportionalElectionUnionId),
            request.ProportionalElectionIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Delete)]
    public override async Task<Empty> Delete(DeleteProportionalElectionUnionRequest request, ServerCallContext context)
    {
        await _proportionalElectionUnionWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Read)]
    public override async Task<ElectionCandidates> GetCandidates(GetProportionalElectionUnionCandidatesRequest request, ServerCallContext context)
    {
        return _mapper.Map<ElectionCandidates>(
            await _proportionalElectionUnionReader.GetCandidates(GuidParser.Parse(request.ProportionalElectionUnionId)));
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Read)]
    public override async Task<PoliticalBusinesses> GetPoliticalBusinesses(GetProportionalElectionUnionPoliticalBusinessesRequest request, ServerCallContext context)
    {
        return _mapper.Map<PoliticalBusinesses>(
            await _proportionalElectionUnionReader.GetPoliticalBusinesses(GuidParser.Parse(request.ProportionalElectionUnionId)));
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Read)]
    public override async Task<ProportionalElectionUnionLists> GetProportionalElectionUnionLists(GetProportionalElectionUnionListsRequest request, ServerCallContext context)
    {
        return _mapper.Map<ProportionalElectionUnionLists>(
            await _proportionalElectionUnionReader.GetUnionLists(GuidParser.Parse(request.ProportionalElectionUnionId)));
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Read)]
    public override async Task<ProportionalElectionUnions> List(ListProportionalElectionUnionsRequest request, ServerCallContext context)
    {
        return _mapper.Map<ProportionalElectionUnions>(
            await _proportionalElectionUnionReader.List(GuidParser.Parse(request.ProportionalElectionId)));
    }

    [AuthorizePermission(Permissions.ProportionalElectionUnion.Update)]
    public override async Task<Empty> UpdatePoliticalBusinesses(UpdateProportionalElectionUnionPoliticalBusinessesRequest request, ServerCallContext context)
    {
        await _proportionalElectionWriter.UpdateAllMandateAlgorithmsInUnion(
            request.ProportionalElectionUnionIds.Select(GuidParser.Parse).ToList(),
            _mapper.Map<ProportionalElectionMandateAlgorithm>(request.MandateAlgorithm));
        return ProtobufEmpty.Instance;
    }
}
