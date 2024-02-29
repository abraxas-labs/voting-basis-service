// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.MajorityElectionUnionService.MajorityElectionUnionServiceBase;

namespace Voting.Basis.Services;

public class MajorityElectionUnionService : ServiceBase
{
    private readonly MajorityElectionUnionReader _majorityElectionUnionReader;
    private readonly MajorityElectionUnionWriter _majorityElectionUnionWriter;
    private readonly IMapper _mapper;

    public MajorityElectionUnionService(
        MajorityElectionUnionReader majorityElectionUnionReader,
        MajorityElectionUnionWriter majorityElectionUnionWriter,
        IMapper mapper)
    {
        _majorityElectionUnionReader = majorityElectionUnionReader;
        _majorityElectionUnionWriter = majorityElectionUnionWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.MajorityElectionUnion.Create)]
    public override async Task<IdValue> Create(CreateMajorityElectionUnionRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionUnion>(request);
        await _majorityElectionUnionWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.MajorityElectionUnion.Update)]
    public override async Task<Empty> Update(UpdateMajorityElectionUnionRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionUnion>(request);
        await _majorityElectionUnionWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionUnion.Update)]
    public override async Task<Empty> UpdateEntries(UpdateMajorityElectionUnionEntriesRequest request, ServerCallContext context)
    {
        await _majorityElectionUnionWriter.UpdateEntries(
            GuidParser.Parse(request.MajorityElectionUnionId),
            request.MajorityElectionIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionUnion.Delete)]
    public override async Task<Empty> Delete(DeleteMajorityElectionUnionRequest request, ServerCallContext context)
    {
        await _majorityElectionUnionWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionUnion.Read)]
    public override async Task<ElectionCandidates> GetCandidates(GetMajorityElectionUnionCandidatesRequest request, ServerCallContext context)
    {
        return _mapper.Map<ElectionCandidates>(
            await _majorityElectionUnionReader.GetCandidates(GuidParser.Parse(request.MajorityElectionUnionId)));
    }

    [AuthorizePermission(Permissions.MajorityElectionUnion.Read)]
    public override async Task<PoliticalBusinesses> GetPoliticalBusinesses(GetMajorityElectionUnionPoliticalBusinessesRequest request, ServerCallContext context)
    {
        return _mapper.Map<PoliticalBusinesses>(
            await _majorityElectionUnionReader.GetPoliticalBusinesses(GuidParser.Parse(request.MajorityElectionUnionId)));
    }
}
