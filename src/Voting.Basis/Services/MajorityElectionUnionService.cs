// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Write;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.MajorityElectionUnionService.MajorityElectionUnionServiceBase;

namespace Voting.Basis.Services;

[Authorize]
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

    public override async Task<IdValue> Create(CreateMajorityElectionUnionRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionUnion>(request);
        await _majorityElectionUnionWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    public override async Task<Empty> Update(UpdateMajorityElectionUnionRequest request, ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionUnion>(request);
        await _majorityElectionUnionWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> UpdateEntries(UpdateMajorityElectionUnionEntriesRequest request, ServerCallContext context)
    {
        await _majorityElectionUnionWriter.UpdateEntries(
            GuidParser.Parse(request.MajorityElectionUnionId),
            request.MajorityElectionIds.Select(GuidParser.Parse).ToList());
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> Delete(DeleteMajorityElectionUnionRequest request, ServerCallContext context)
    {
        await _majorityElectionUnionWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ElectionCandidates> GetCandidates(GetMajorityElectionUnionCandidatesRequest request, ServerCallContext context)
    {
        return _mapper.Map<ElectionCandidates>(
            await _majorityElectionUnionReader.GetCandidates(GuidParser.Parse(request.MajorityElectionUnionId)));
    }

    public override async Task<PoliticalBusinesses> GetPoliticalBusinesses(GetMajorityElectionUnionPoliticalBusinessesRequest request, ServerCallContext context)
    {
        return _mapper.Map<PoliticalBusinesses>(
            await _majorityElectionUnionReader.GetPoliticalBusinesses(GuidParser.Parse(request.MajorityElectionUnionId)));
    }
}
