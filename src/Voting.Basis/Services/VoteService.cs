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
using ServiceBase = Abraxas.Voting.Basis.Services.V1.VoteService.VoteServiceBase;

namespace Voting.Basis.Services;

public class VoteService : ServiceBase
{
    private readonly VoteReader _voteReader;
    private readonly VoteWriter _voteWriter;
    private readonly IMapper _mapper;

    public VoteService(
        VoteReader voteReader,
        VoteWriter voteWriter,
        IMapper mapper)
    {
        _voteReader = voteReader;
        _voteWriter = voteWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.Vote.Create)]
    public override async Task<IdValue> Create(
        CreateVoteRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Vote>(request);
        await _voteWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.Vote.Update)]
    public override async Task<Empty> Update(
        UpdateVoteRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Vote>(request);
        await _voteWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.Vote.Update)]
    public override async Task<Empty> UpdateActiveState(
        UpdateVoteActiveStateRequest request,
        ServerCallContext context)
    {
        await _voteWriter.UpdateActiveState(GuidParser.Parse(request.Id), request.Active);
        return ProtobufEmpty.Instance;
    }

    [AuthorizeAnyPermission(Permissions.Vote.EVotingApprove, Permissions.Vote.EVotingApproveRevert)]
    public override async Task<Empty> UpdateEVotingApproval(UpdateVoteEVotingApprovalRequest request, ServerCallContext context)
    {
        await _voteWriter.UpdateEVotingApproval(GuidParser.Parse(request.Id), request.Approved);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.Vote.Delete)]
    public override async Task<Empty> Delete(
        DeleteVoteRequest request,
        ServerCallContext context)
    {
        await _voteWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.Vote.Read)]
    public override async Task<Vote> Get(
        GetVoteRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<Vote>(await _voteReader.Get(GuidParser.Parse(request.Id)));
    }

    [AuthorizePermission(Permissions.VoteBallot.Create)]
    public override async Task<IdValue> CreateBallot(
        CreateBallotRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Ballot>(request);
        await _voteWriter.CreateBallot(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.VoteBallot.Update)]
    public override async Task<Empty> UpdateBallot(
        UpdateBallotRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Ballot>(request);
        await _voteWriter.UpdateBallot(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.VoteBallot.Delete)]
    public override async Task<Empty> DeleteBallot(
        DeleteBallotRequest request,
        ServerCallContext context)
    {
        await _voteWriter.DeleteBallot(GuidParser.Parse(request.Id), GuidParser.Parse(request.VoteId));
        return ProtobufEmpty.Instance;
    }
}
