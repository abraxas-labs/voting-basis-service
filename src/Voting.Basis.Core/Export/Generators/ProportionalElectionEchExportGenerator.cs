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
                .ThenInclude(c => c.DomainOfInfluence)
            .Include(pe => pe.ProportionalElectionLists)
                .ThenInclude(l => l.ProportionalElectionCandidates)
                    .ThenInclude(c => c.Party)
            .Include(pe => pe.ProportionalElectionListUnions)
                .ThenInclude(lu => lu.ProportionalElectionListUnionEntries)
            .Where(pe => pe.Id == electionId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(electionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId, true);

        var xmlBytes = _ech0157Serializer.ToDelivery(proportionalElection.Contest, proportionalElection);
        var electionDescription = LanguageUtil.GetInCurrentLanguage(proportionalElection.ShortDescription);
        var fileName = FileNameUtil.GetXmlFileName(Ech0157Serializer.EchNumber, Ech0157Serializer.EchVersion, proportionalElection.Contest.DomainOfInfluence.Canton, proportionalElection.Contest.Date, electionDescription);
        return new ExportFile(xmlBytes, fileName, MediaTypeNames.Application.Xml);
    }
}
