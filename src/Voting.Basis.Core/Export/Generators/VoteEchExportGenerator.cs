﻿// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators;

public class VoteEchExportGenerator : IExportGenerator
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly Ech159Serializer _ech159Serializer;
    private readonly PermissionService _permissionService;

    public VoteEchExportGenerator(IDbRepository<DataContext, Vote> voteRepo, Ech159Serializer ech159Serializer, PermissionService permissionService)
    {
        _voteRepo = voteRepo;
        _ech159Serializer = ech159Serializer;
        _permissionService = permissionService;
    }

    public TemplateModel Template => BasisXmlVoteTemplates.Ech0159;

    public async Task<ExportFile> GenerateExport(Guid voteId)
    {
        var vote = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(v => v.Contest)
            .Include(v => v.DomainOfInfluence)
            .Include(v => v.Ballots)
                .ThenInclude(b => b.BallotQuestions)
            .Include(v => v.Ballots)
                .ThenInclude(b => b.TieBreakQuestions)
            .Where(v => v.Id == voteId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(voteId);

        var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
        if (!doiHierarchyGroups.TenantAndChildDoiIds.Contains(vote.DomainOfInfluenceId))
        {
            throw new ForbiddenException();
        }

        var ech159 = _ech159Serializer.ToEventInitialDelivery(vote.Contest, vote);
        var xmlBytes = EchSerializer.ToXml(ech159);
        var voteDescription = LanguageUtil.GetInCurrentLanguage(vote.ShortDescription);
        return new ExportFile(xmlBytes, voteDescription + FileExtensions.Xml, MediaTypeNames.Application.Xml);
    }
}
