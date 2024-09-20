// (c) Copyright by Abraxas Informatik AG
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
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ElectionGroupService.ElectionGroupServiceBase;

namespace Voting.Basis.Services;

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

    [AuthorizePermission(Permissions.ElectionGroup.Update)]
    public override async Task<Empty> Update(
        UpdateElectionGroupRequest request,
        ServerCallContext context)
    {
        await _electionGroupWriter.Update(GuidParser.Parse(request.PrimaryMajorityElectionId), request.Description);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.ElectionGroup.Read)]
    public override async Task<ElectionGroups> List(
        ListElectionGroupsRequest request,
        ServerCallContext context)
    {
        var electionGroups = await _electionGroupReader.List(GuidParser.Parse(request.ContestId));
        return _mapper.Map<ElectionGroups>(electionGroups);
    }
}
