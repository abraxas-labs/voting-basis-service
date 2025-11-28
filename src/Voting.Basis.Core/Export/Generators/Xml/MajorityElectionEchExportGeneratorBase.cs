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

public class MajorityElectionEchExportGeneratorBase
{
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly EchSerializerProvider _echSerializerProvider;
    private readonly PermissionService _permissionService;

    public MajorityElectionEchExportGeneratorBase(
        IDbRepository<DataContext, MajorityElection> electionRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService)
    {
        _electionRepo = electionRepo;
        _echSerializerProvider = echSerializerProvider;
        _permissionService = permissionService;
    }

    public async Task<ExportFile> GenerateExport(Guid entityId, bool v5)
    {
        var majorityElection = await _electionRepo.Query()
            .AsSplitQuery()
            .Include(me => me.Contest)
                .ThenInclude(c => c.DomainOfInfluence)
            .Include(me => me.MajorityElectionCandidates)
            .Include(me => me.SecondaryMajorityElections)
                .ThenInclude(se => se.Candidates)
            .Include(me => me.DomainOfInfluence)
            .Where(me => me.Id == entityId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(entityId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(majorityElection.DomainOfInfluenceId, true);
        var ech0157Serializer = _echSerializerProvider.GetEch0157Serializer(v5);

        var xmlBytes = await ech0157Serializer.ToDelivery(majorityElection.Contest, majorityElection);

        var electionDescription = LanguageUtil.GetInCurrentLanguage(majorityElection.ShortDescription);

        var fileName = FileNameUtil.GetXmlFileName(
            ech0157Serializer.EchNumber,
            ech0157Serializer.EchVersion,
            majorityElection.Contest.DomainOfInfluence.Canton,
            majorityElection.Contest.Date,
            electionDescription);

        return new ExportFile(xmlBytes, fileName, MediaTypeNames.Application.Xml);
    }
}
