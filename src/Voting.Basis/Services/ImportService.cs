﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Voting.Lib.Common;
using Voting.Lib.Grpc;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ImportService.ImportServiceBase;

namespace Voting.Basis.Services;

[Authorize]
public class ImportService : ServiceBase
{
    private readonly IMapper _mapper;
    private readonly Core.Import.ImportService _importService;
    private readonly Core.Import.ProportionalElectionListsAndCandidatesImportService _proportionalElectionListsAndCandidatesImportService;
    private readonly Core.Import.MajorityElectionCandidatesImportService _majorityElectionCandidatesImportService;

    public ImportService(
        Core.Import.ImportService importService,
        Core.Import.ProportionalElectionListsAndCandidatesImportService proportionalElectionListsAndCandidatesImportService,
        Core.Import.MajorityElectionCandidatesImportService majorityElectionCandidatesImportService,
        IMapper mapper)
    {
        _importService = importService;
        _proportionalElectionListsAndCandidatesImportService = proportionalElectionListsAndCandidatesImportService;
        _majorityElectionCandidatesImportService = majorityElectionCandidatesImportService;
        _mapper = mapper;
    }

    public override Task<ContestImport> ResolveImportFile(ResolveImportFileRequest request, ServerCallContext context)
    {
        var contestImport = _importService.DeserializeImport(request.ImportType, request.FileContent, context.CancellationToken);
        return Task.FromResult(_mapper.Map<ContestImport>(contestImport));
    }

    public override async Task<Empty> ImportContest(ImportContestRequest request, ServerCallContext context)
    {
        var import = _mapper.Map<Core.Import.ContestImport>(request.Contest);
        await _importService.Import(import);
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> ImportPoliticalBusinesses(ImportPoliticalBusinessesRequest request, ServerCallContext context)
    {
        await _importService.Import(
            GuidParser.Parse(request.ContestId),
            _mapper.Map<List<Core.Import.MajorityElectionImport>>(request.MajorityElections),
            _mapper.Map<List<Core.Import.ProportionalElectionImport>>(request.ProportionalElections),
            _mapper.Map<List<Core.Import.VoteImport>>(request.Votes));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> ImportProportionalElectionListsAndCandidates(ImportProportionalElectionListsAndCandidatesRequest request, ServerCallContext context)
    {
        await _proportionalElectionListsAndCandidatesImportService.Import(
            GuidParser.Parse(request.ProportionalElectionId),
            _mapper.Map<List<Core.Import.ProportionalElectionListImport>>(request.Lists),
            _mapper.Map<List<Core.Domain.ProportionalElectionListUnion>>(request.ListUnions));
        return ProtobufEmpty.Instance;
    }

    public override async Task<Empty> ImportMajorityElectionCandidates(ImportMajorityElectionCandidatesRequest request, ServerCallContext context)
    {
        var candidates = _mapper.Map<IReadOnlyCollection<Core.Domain.MajorityElectionCandidate>>(request.Candidates);
        await _majorityElectionCandidatesImportService.Import(GuidParser.Parse(request.MajorityElectionId), candidates);
        return ProtobufEmpty.Instance;
    }
}
