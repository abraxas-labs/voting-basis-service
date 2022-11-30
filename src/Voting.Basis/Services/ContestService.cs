// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
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
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using Contest = Abraxas.Voting.Basis.Services.V1.Models.Contest;
using ContestCountingCircleOption = Voting.Basis.Core.Domain.ContestCountingCircleOption;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ContestService.ContestServiceBase;

namespace Voting.Basis.Services;

[Authorize]
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

    public override async Task<IdValue> Create(
        CreateContestRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Contest>(request);
        await _contestWriter.Create(data);
        return new IdValue { Id = data.Id.ToString() };
    }

    public override async Task<Empty> Update(
        UpdateContestRequest request,
        ServerCallContext context)
    {
        var data = _mapper.Map<Core.Domain.Contest>(request);
        await _contestWriter.Update(data);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> Delete(DeleteContestRequest request, ServerCallContext context)
    {
        await _contestWriter.Delete(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Contest> Get(
        GetContestRequest request,
        ServerCallContext context)
        => _mapper.Map<Contest>(await _contestReader.Get(GuidParser.Parse(request.Id)));

    public override async Task<ContestSummaries> ListSummaries(ListContestSummariesRequest request, ServerCallContext context)
    {
        var contestSummaries = await _contestReader.ListSummaries(request.States.Cast<ContestState>().ToList());
        return _mapper.Map<ContestSummaries>(contestSummaries);
    }

    public override async Task<PreconfiguredContestDates> ListFuturePreconfiguredDates(
        ListFuturePreconfiguredDatesRequest request, ServerCallContext context)
    {
        var preconfiguredDates = await _contestReader.ListFuturePreconfiguredDates();
        return _mapper.Map<PreconfiguredContestDates>(preconfiguredDates);
    }

    public override async Task<Contests> ListPast(ListContestPastRequest request, ServerCallContext context)
    {
        var contests = await _contestReader.ListPast(request.Date.ToDateTime(), GuidParser.Parse(request.DomainOfInfluenceId));
        return _mapper.Map<Contests>(contests);
    }

    public override async Task<ContestAvailability> CheckAvailability(CheckAvailabilityRequest request, ServerCallContext context)
    {
        var availability = await _contestReader.CheckAvailability(request.Date.ToDateTime(), GuidParser.Parse(request.DomainOfInfluenceId));
        return new ContestAvailability { Availability = availability };
    }

    public override async Task<Empty> Archive(ArchiveContestRequest request, ServerCallContext context)
    {
        await _contestWriter.Archive(GuidParser.Parse(request.Id), request.ArchivePer?.ToDateTime());
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> PastUnlock(PastUnlockContestRequest request, ServerCallContext context)
    {
        await _contestWriter.PastUnlock(GuidParser.Parse(request.Id));
        return ProtobufEmpty.Instance;
    }

    public override async Task<ContestCountingCircleOptions> ListCountingCircleOptions(
        ListCountingCircleOptionsRequest request,
        ServerCallContext context)
    {
        var options = await _contestReader.ListCountingCircleOptions(GuidParser.Parse(request.Id));
        return _mapper.Map<ContestCountingCircleOptions>(options);
    }

    public override async Task<Empty> UpdateCountingCircleOptions(UpdateCountingCircleOptionsRequest request, ServerCallContext context)
    {
        await _contestWriter.UpdateCountingCircleOptions(GuidParser.Parse(request.Id), _mapper.Map<List<ContestCountingCircleOption>>(request.Options));
        return ProtobufEmpty.Instance;
    }

    public override Task GetOverviewChanges(
        GetContestOverviewChangesRequest request,
        IServerStreamWriter<ContestOverviewChangeMessage> responseStream,
        ServerCallContext context)
    {
        return _contestReader.ListenToContestOverviewChanges(
            e => responseStream.WriteAsync(_mapper.Map<ContestOverviewChangeMessage>(e)),
            context.CancellationToken);
    }

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
