// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

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
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ElectionGroupService.ElectionGroupServiceBase;

namespace Voting.Basis.Services;

[Authorize]
public class ElectionGroupService : ServiceBase
{
    private readonly ElectionGroupReader _electionGroupReader;
    private readonly ElectionGroupWriter _electionGroupWriter;
    private readonly IMapper _mapper;

    public ElectionGroupService(
        ElectionGroupReader electionGroupReader,
        ElectionGroupWriter electionGroupWriter,
        IMapper mapper)
    {
        _electionGroupReader = electionGroupReader;
        _electionGroupWriter = electionGroupWriter;
        _mapper = mapper;
    }

    public override async Task<Empty> Update(
        UpdateElectionGroupRequest request,
        ServerCallContext context)
    {
        await _electionGroupWriter.Update(GuidParser.Parse(request.PrimaryMajorityElectionId), request.Description);
        return ProtobufEmpty.Instance;
    }

    public override async Task<ElectionGroups> List(
        ListElectionGroupsRequest request,
        ServerCallContext context)
    {
        var electionGroups = await _electionGroupReader.List(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ElectionGroups>(electionGroups);
    }
}
