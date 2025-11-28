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

public class ProportionalElectionEchExportGeneratorBase
{
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly EchSerializerProvider _echSerializerProvider;
    private readonly PermissionService _permissionService;

    public ProportionalElectionEchExportGeneratorBase(
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService)
    {
        _electionRepo = electionRepo;
        _echSerializerProvider = echSerializerProvider;
        _permissionService = permissionService;
    }

    public async Task<ExportFile> GenerateExport(Guid entityId, bool v5)
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
            .Include(pe => pe.DomainOfInfluence)
            .Where(pe => pe.Id == entityId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(entityId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(proportionalElection.DomainOfInfluenceId, true);
        var ech0157Serializer = _echSerializerProvider.GetEch0157Serializer(v5);

        var xmlBytes = await ech0157Serializer.ToDelivery(proportionalElection.Contest, proportionalElection);

        var electionDescription = LanguageUtil.GetInCurrentLanguage(proportionalElection.ShortDescription);

        var fileName = FileNameUtil.GetXmlFileName(
            ech0157Serializer.EchNumber,
            ech0157Serializer.EchVersion,
            proportionalElection.Contest.DomainOfInfluence.Canton,
            proportionalElection.Contest.Date,
            electionDescription);

        return new ExportFile(xmlBytes, fileName, MediaTypeNames.Application.Xml);
    }
}
