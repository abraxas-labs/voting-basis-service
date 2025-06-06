﻿// (c) Copyright by Abraxas Informatik AG
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
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators;

public class MajorityElectionEchExportGenerator : IExportGenerator
{
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly Ech0157Serializer _ech0157Serializer;
    private readonly PermissionService _permissionService;

    public MajorityElectionEchExportGenerator(
        IDbRepository<DataContext, MajorityElection> electionRepo,
        Ech0157Serializer ech0157Serializer,
        PermissionService permissionService)
    {
        _electionRepo = electionRepo;
        _ech0157Serializer = ech0157Serializer;
        _permissionService = permissionService;
    }

    public TemplateModel Template => BasisXmlMajorityElectionTemplates.Ech0157;

    public async Task<ExportFile> GenerateExport(Guid electionId)
    {
        var majorityElection = await _electionRepo.Query()
            .AsSplitQuery()
            .Include(me => me.Contest)
                .ThenInclude(c => c.DomainOfInfluence)
            .Include(me => me.MajorityElectionCandidates)
            .Include(me => me.SecondaryMajorityElections)
                .ThenInclude(se => se.Candidates)
            .Where(me => me.Id == electionId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(electionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId, true);

        var xmlBytes = await _ech0157Serializer.ToDelivery(majorityElection.Contest, majorityElection);
        var electionDescription = LanguageUtil.GetInCurrentLanguage(majorityElection.ShortDescription);
        var fileName = FileNameUtil.GetXmlFileName(Ech0157Serializer.EchNumber, Ech0157Serializer.EchVersion, majorityElection.Contest.DomainOfInfluence.Canton, majorityElection.Contest.Date, electionDescription);
        return new ExportFile(xmlBytes, fileName, MediaTypeNames.Application.Xml);
    }
}
