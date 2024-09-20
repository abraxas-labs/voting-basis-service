// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
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
using EntityOrder = Voting.Basis.Core.Domain.EntityOrder;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.MajorityElectionService.MajorityElectionServiceBase;

namespace Voting.Basis.Services;

public class MajorityElectionService : ServiceBase
{
    private readonly MajorityElectionReader _majorityElectionReader;
    private readonly MajorityElectionWriter _majorityElectionWriter;
    private readonly IMapper _mapper;

    public MajorityElectionService(
        MajorityElectionReader majorityElectionReader,
        MajorityElectionWriter majorityElectionWriter,
        IMapper mapper)
    {
        _majorityElectionReader = majorityElectionReader;
        _majorityElectionWriter = majorityElectionWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.MajorityElection.Create)]
    public override async Task<IdValue> Create(
        CreateMajorityElectionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElection>(request);
        await _majorityElectionWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.MajorityElection.Update)]
    public override async Task<Empty> Update(
        UpdateMajorityElectionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElection>(request);
        await _majorityElectionWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElection.Update)]
    public override async Task<Empty> UpdateActiveState(
        UpdateMajorityElectionActiveStateRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.UpdateActiveState(GuidParser.Parse(request.Id), request.Active);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElection.Delete)]
    public override async Task<Empty> Delete(
        DeleteMajorityElectionRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElection.Read)]
    public override async Task<MajorityElection> Get(
        GetMajorityElectionRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<MajorityElection>(await _majorityElectionReader.Get(GuidParser.Parse(request.Id)));
    }

    [AuthorizePermission(Permissions.MajorityElection.Read)]
    public override async Task<MajorityElections> List(
        ListMajorityElectionRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<MajorityElections>(await _majorityElectionReader.ListOwnedByTenantForContest(GuidParser.Parse(request.ContestId)));
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Create)]
    public override async Task<IdValue> CreateCandidate(
        CreateMajorityElectionCandidateRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionCandidate>(request);
        await _majorityElectionWriter.CreateCandidate(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Read)]
    public override async Task<MajorityElectionCandidates> ListCandidates(
        ListMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var candidates = await _majorityElectionReader.GetCandidates(GuidParser.Parse(request.MajorityElectionId));
        return _mapper.Map<MajorityElectionCandidates>(candidates);
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Read)]
    public override async Task<MajorityElectionCandidate> GetCandidate(
        GetMajorityElectionCandidateRequest request,
        ServerCallContext context)
    {
        var candidate = await _majorityElectionReader.GetCandidate(GuidParser.Parse(request.Id));
        return _mapper.Map<MajorityElectionCandidate>(candidate);
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Update)]
    public override async Task<Empty> UpdateCandidate(
        UpdateMajorityElectionCandidateRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionCandidate>(request);
        await _majorityElectionWriter.UpdateCandidate(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Update)]
    public override async Task<Empty> ReorderCandidates(
        ReorderMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.ReorderCandidates(
            GuidParser.Parse(request.MajorityElectionId),
            _mapper.Map<IReadOnlyCollection<EntityOrder>>(request.Orders.Orders));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionCandidate.Delete)]
    public override async Task<Empty> DeleteCandidate(
        DeleteMajorityElectionCandidateRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.DeleteCandidate(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElection.Create)]
    public override async Task<IdValue> CreateSecondaryMajorityElection(
        CreateSecondaryMajorityElectionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.SecondaryMajorityElection>(request);
        await _majorityElectionWriter.CreateSecondaryMajorityElection(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElection.Update)]
    public override async Task<Empty> UpdateSecondaryMajorityElection(
        UpdateSecondaryMajorityElectionRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.SecondaryMajorityElection>(request);
        await _majorityElectionWriter.UpdateSecondaryMajorityElection(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElection.Read)]
    public override async Task<SecondaryMajorityElections> ListSecondaryMajorityElections(
        ListSecondaryMajorityElectionsRequest request,
        ServerCallContext context)
    {
        var secondaryMajorityElections = await _majorityElectionReader.GetSecondaryMajorityElections(GuidParser.Parse(request.MajorityElectionId));
        return _mapper.Map<SecondaryMajorityElections>(secondaryMajorityElections);
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElection.Read)]
    public override async Task<SecondaryMajorityElection> GetSecondaryMajorityElection(
        GetSecondaryMajorityElectionRequest request,
        ServerCallContext context)
    {
        var secondaryMajorityElection = await _majorityElectionReader.GetSecondaryMajorityElection(GuidParser.Parse(request.Id));
        return _mapper.Map<SecondaryMajorityElection>(secondaryMajorityElection);
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElection.Delete)]
    public override async Task<Empty> DeleteSecondaryMajorityElection(
        DeleteSecondaryMajorityElectionRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.DeleteSecondaryMajorityElection(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElection.Update)]
    public override async Task<Empty> UpdateSecondaryMajorityElectionActiveState(
        UpdateSecondaryMajorityElectionActiveStateRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.UpdateSecondaryMajorityElectionActiveState(GuidParser.Parse(request.Id), request.Active);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Create)]
    public override async Task<IdValue> CreateSecondaryMajorityElectionCandidate(
        CreateSecondaryMajorityElectionCandidateRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionCandidate>(request);
        await _majorityElectionWriter.CreateSecondaryMajorityElectionCandidate(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Update)]
    public override async Task<Empty> UpdateSecondaryMajorityElectionCandidate(
        UpdateSecondaryMajorityElectionCandidateRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionCandidate>(request);
        await _majorityElectionWriter.UpdateSecondaryMajorityElectionCandidate(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Delete)]
    public override async Task<Empty> DeleteSecondaryMajorityElectionCandidate(
        DeleteSecondaryMajorityElectionCandidateRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.DeleteSecondaryMajorityElectionCandidate(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Read)]
    public override async Task<SecondaryMajorityElectionCandidates> ListSecondaryMajorityElectionCandidates(
        ListSecondaryMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        var candidates = await _majorityElectionReader.GetSecondaryMajorityElectionCandidates(GuidParser.Parse(request.SecondaryMajorityElectionId));
        return _mapper.Map<SecondaryMajorityElectionCandidates>(candidates);
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Create)]
    public override async Task<IdValue> CreateMajorityElectionCandidateReference(
        CreateMajorityElectionCandidateReferenceRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionCandidateReference>(request);
        await _majorityElectionWriter.CreateMajorityElectionCandidateReference(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Update)]
    public override async Task<Empty> UpdateMajorityElectionCandidateReference(
        UpdateMajorityElectionCandidateReferenceRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionCandidateReference>(request);
        await _majorityElectionWriter.UpdateMajorityElectionCandidateReference(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Delete)]
    public override async Task<Empty> DeleteMajorityElectionCandidateReference(
        DeleteMajorityElectionCandidateReferenceRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.DeleteMajorityElectionCandidateReference(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.SecondaryMajorityElectionCandidate.Update)]
    public override async Task<Empty> ReorderSecondaryMajorityElectionCandidates(
        ReorderSecondaryMajorityElectionCandidatesRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.ReorderSecondaryMajorityElectionCandidates(
            GuidParser.Parse(request.SecondaryMajorityElectionId),
            _mapper.Map<IReadOnlyCollection<EntityOrder>>(request.Orders.Orders));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionBallotGroup.Create)]
    public override async Task<MajorityElectionBallotGroup> CreateBallotGroup(
        CreateMajorityElectionBallotGroupRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionBallotGroup>(request);
        await _majorityElectionWriter.CreateBallotGroup(data);
        return _mapper.Map<MajorityElectionBallotGroup>(data);
    }

    [AuthorizePermission(Permissions.MajorityElectionBallotGroup.Update)]
    public override async Task<MajorityElectionBallotGroup> UpdateBallotGroup(
        UpdateMajorityElectionBallotGroupRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionBallotGroup>(request);
        await _majorityElectionWriter.UpdateBallotGroup(data);
        return _mapper.Map<MajorityElectionBallotGroup>(data);
    }

    [AuthorizePermission(Permissions.MajorityElectionBallotGroup.Read)]
    public override async Task<MajorityElectionBallotGroups> ListBallotGroups(
        ListMajorityElectionBallotGroupsRequest request,
        ServerCallContext context)
    {
        var ballotGroups = await _majorityElectionReader.GetBallotGroups(GuidParser.Parse(request.MajorityElectionId));
        return _mapper.Map<MajorityElectionBallotGroups>(ballotGroups);
    }

    [AuthorizePermission(Permissions.MajorityElectionBallotGroup.Delete)]
    public override async Task<Empty> DeleteBallotGroup(
        DeleteMajorityElectionBallotGroupRequest request,
        ServerCallContext context)
    {
        await _majorityElectionWriter.DeleteBallotGroup(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.MajorityElectionBallotGroup.Read)]
    public override async Task<MajorityElectionBallotGroupCandidates> ListBallotGroupCandidates(
        ListMajorityElectionBallotGroupCandidatesRequest request,
        ServerCallContext context)
    {
        var ballotGroupCandidates = await _majorityElectionReader.GetBallotGroupEntriesWithCandidates(GuidParser.Parse(request.BallotGroupId));
        var candidates = _mapper.Map<MajorityElectionBallotGroupCandidates>(ballotGroupCandidates);
        candidates.BallotGroupId = request.BallotGroupId;
        return candidates;
    }

    [AuthorizePermission(Permissions.MajorityElectionBallotGroup.Update)]
    public override async Task<Empty> UpdateBallotGroupCandidates(
        UpdateMajorityElectionBallotGroupCandidatesRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.MajorityElectionBallotGroupCandidates>(request);
        await _majorityElectionWriter.UpdateBallotGroupCandidates(data);
        return ProtobufEmpty.Instance;
    }
}
