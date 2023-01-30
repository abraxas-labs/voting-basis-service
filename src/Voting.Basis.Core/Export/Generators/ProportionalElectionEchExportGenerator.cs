// (c) Copyright 2022 by Abraxas Informatik AG
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

public class ProportionalElectionEchExportGenerator : IExportGenerator
{
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly Ech0157Serializer _ech0157Serializer;
    private readonly PermissionService _permissionService;

    public ProportionalElectionEchExportGenerator(
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        Ech0157Serializer ech0157Serializer,
        PermissionService permissionService)
    {
        _electionRepo = electionRepo;
        _ech0157Serializer = ech0157Serializer;
        _permissionService = permissionService;
    }

    public TemplateModel Template => BasisXmlProportionalElectionTemplates.Ech0157;

    public async Task<ExportFile> GenerateExport(Guid electionId)
    {
        var proportionalElection = await _electionRepo.Query()
            .AsSplitQuery()
            .Include(pe => pe.Contest)
            .Include(pe => pe.ProportionalElectionLists)
                .ThenInclude(l => l.ProportionalElectionCandidates)
                    .ThenInclude(c => c.Party)
            .Include(pe => pe.ProportionalElectionListUnions)
                .ThenInclude(lu => lu.ProportionalElectionListUnionEntries)
            .Where(pe => pe.Id == electionId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(electionId);

        var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
        if (!doiHierarchyGroups.TenantAndChildDoiIds.Contains(proportionalElection.DomainOfInfluenceId))
        {
            throw new ForbiddenException();
        }

        var ech0157 = _ech0157Serializer.ToDelivery(proportionalElection.Contest, proportionalElection);
        var xmlBytes = EchSerializer.ToXml(ech0157);
        var electionDescription = LanguageUtil.GetInCurrentLanguage(proportionalElection.ShortDescription);
        return new ExportFile(xmlBytes, electionDescription + FileExtensions.Xml, MediaTypeNames.Application.Xml);
    }
}
