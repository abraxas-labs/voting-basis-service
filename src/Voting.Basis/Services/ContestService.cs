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
using Contest = Abraxas.Voting.Basis.Services.V1.Models.Contest;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ContestService.ContestServiceBase;

namespace Voting.Basis.Services;

public class ContestService : ServiceBase
{
    private readonly ContestReader _contestReader;
    private readonly ContestWriter _contestWriter;
    private readonly IMapper _mapper;

    public ContestService(
        ContestReader contestReader,
        ContestWriter contestWriter,
        IMapper mapper)
    {
        _contestReader = contestReader;
        _contestWriter = contestWriter;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.Contest.Create)]
    public override async Task<IdValue> Create(
        CreateContestRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Contest>(request);
        await _contestWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    [AuthorizePermission(Permissions.Contest.Update)]
    public override async Task<Empty> Update(
        UpdateContestRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Contest>(request);
        await _contestWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.Contest.Delete)]
    public override async Task<Empty> Delete(DeleteContestRequest request, ServerCallContext context)
    {
        await _contestWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizeAnyPermission(Permissions.Contest.ReadTenantHierarchy, Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll)]
    public override async Task<Contest> Get(
        GetContestRequest request,
        ServerCallContext context)
    {
        var contest = await _contestReader.Get(GuidParser.Parse(request.Id));
        return _mapper.Map<Contest>(contest);
    }

    [AuthorizeAnyPermission(Permissions.Contest.ReadTenantHierarchy, Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll)]
    public override async Task<ContestSummaries> ListSummaries(ListContestSummariesRequest request, ServerCallContext context)
    {
        var contestSummaries = await _contestReader.ListSummaries(request.States.Cast<ContestState>().ToList());
        return _mapper.Map<ContestSummaries>(contestSummaries);
    }

    [AuthorizeAnyPermission(Permissions.Contest.ReadTenantHierarchy, Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll)]
    public override async Task<PreconfiguredContestDates> ListFuturePreconfiguredDates(
        ListFuturePreconfiguredDatesRequest request, ServerCallContext context)
    {
        var preconfiguredDates = await _contestReader.ListFuturePreconfiguredDates();
        return _mapper.Map<PreconfiguredContestDates>(preconfiguredDates);
    }

    [AuthorizeAnyPermission(Permissions.Contest.ReadTenantHierarchy, Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll)]
    public override async Task<Contests> ListPast(ListContestPastRequest request, ServerCallContext context)
    {
        var contests = await _contestReader.ListPast(request.Date.ToDateTime(), GuidParser.Parse(request.DomainOfInfluenceId));
        return _mapper.Map<Contests>(contests);
    }

    [AuthorizeAnyPermission(Permissions.Contest.ReadTenantHierarchy, Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll)]
    public override async Task<ContestAvailability> CheckAvailability(CheckAvailabilityRequest request, ServerCallContext context)
    {
        var availability = await _contestReader.CheckAvailability(request.Date.ToDateTime(), GuidParser.Parse(request.DomainOfInfluenceId));
        return new ContestAvailability { Availability = availability };
    }

    [AuthorizePermission(Permissions.Contest.Update)]
    public override async Task<Empty> Archive(ArchiveContestRequest request, ServerCallContext context)
    {
        await _contestWriter.Archive(GuidParser.Parse(request.Id), request.ArchivePer?.ToDateTime());
        return ProtobufEmpty.Instance;
    }

    [AuthorizePermission(Permissions.Contest.Update)]
    public override async Task<Empty> PastUnlock(PastUnlockContestRequest request, ServerCallContext context)
    {
        await _contestWriter.PastUnlock(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    [AuthorizeAnyPermission(Permissions.Contest.ReadTenantHierarchy, Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll)]
    public override Task GetOverviewChanges(
        GetContestOverviewChangesRequest request,
        IServerStreamWriter<ContestOverviewChangeMessage> responseStream,
        ServerCallContext context)
    {
        return _contestReader.ListenToContestOverviewChanges(
            e => responseStream.WriteAsync(_mapper.Map<ContestOverviewChangeMessage>(e)),
            context.CancellationToken);
    }

    [AuthorizeAnyPermission(Permissions.Contest.ReadTenantHierarchy, Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll)]
    public override Task GetDetailsChanges(
        GetContestDetailsChangesRequest request,
        IServerStreamWriter<ContestDetailsChangeMessage> responseStream,
        ServerCallContext context)
    {
        return _contestReader.ListenToContestDetailsChanges(
            GuidParser.Parse(request.Id),
            e => responseStream.WriteAsync(_mapper.Map<ContestDetailsChangeMessage>(e)),
            context.CancellationToken);
    }
}
