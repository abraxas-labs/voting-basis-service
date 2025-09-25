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
using Voting.Basis.Core.Services.Read;
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
    private readonly EchSerializerProvider _echSerializerProvider;
    private readonly PermissionService _permissionService;
    private readonly CountingCircleReader _countingCircleReader;

    public ProportionalElectionEchExportGenerator(
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService,
        CountingCircleReader countingCircleReader)
    {
        _electionRepo = electionRepo;
        _echSerializerProvider = echSerializerProvider;
        _permissionService = permissionService;
        _countingCircleReader = countingCircleReader;
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
            .Include(pe => pe.DomainOfInfluence)
            .Where(pe => pe.Id == electionId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(electionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(proportionalElection.DomainOfInfluenceId, true);
        var eCounting = await _countingCircleReader.OwnsAnyECountingCountingCircle();
        var ech0157Serializer = _echSerializerProvider.GetEch0157Serializer(eCounting);

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
