// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Export.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Export.Generators.Xml;

public abstract class VoteEchExportGeneratorBase
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly EchSerializerProvider _echSerializerProvider;
    private readonly PermissionService _permissionService;

    public VoteEchExportGeneratorBase(
        IDbRepository<DataContext, Vote> voteRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService)
    {
        _voteRepo = voteRepo;
        _echSerializerProvider = echSerializerProvider;
        _permissionService = permissionService;
    }

    public async Task<ExportFile> GenerateExport(Guid entityId, bool v5)
    {
        var vote = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(v => v.Contest)
            .Include(v => v.DomainOfInfluence)
            .Include(v => v.Ballots)
                .ThenInclude(b => b.BallotQuestions)
            .Include(v => v.Ballots)
                .ThenInclude(b => b.TieBreakQuestions)
            .Where(v => v.Id == entityId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(entityId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(vote.DomainOfInfluenceId, true);
        var ech0159Serializer = _echSerializerProvider.GetEch0159Serializer(v5);

        var xmlBytes = await ech0159Serializer.ToEventInitialDelivery(vote.Contest, vote);

        var voteDescription = LanguageUtil.GetInCurrentLanguage(vote.ShortDescription);

        var fileName = FileNameUtil.GetXmlFileName(
            ech0159Serializer.EchNumber,
            ech0159Serializer.EchVersion,
            vote.DomainOfInfluence!.Canton,
            vote.Contest.Date,
            voteDescription);

        return new ExportFile(xmlBytes, fileName, MediaTypeNames.Application.Xml);
    }
}
