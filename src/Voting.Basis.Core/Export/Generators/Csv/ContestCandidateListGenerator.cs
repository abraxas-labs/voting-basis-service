// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Export.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators.Csv;

public class ContestCandidateListGenerator : IExportGenerator
{
    private readonly IAuth _auth;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly MajorityElectionCandidateListGenerator _majorityElectionGenerator;
    private readonly ProportionalElectionCandidateListGenerator _proportionalElectionGenerator;
    private readonly PermissionService _permissionService;
    private readonly CsvService _csvService;

    public ContestCandidateListGenerator(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        MajorityElectionCandidateListGenerator majorityElectionGenerator,
        ProportionalElectionCandidateListGenerator proportionalElectionGenerator,
        PermissionService permissionService,
        CsvService csvService)
    {
        _auth = auth;
        _contestRepo = contestRepo;
        _majorityElectionGenerator = majorityElectionGenerator;
        _proportionalElectionGenerator = proportionalElectionGenerator;
        _permissionService = permissionService;
        _csvService = csvService;
    }

    public TemplateModel Template => BasisCsvContestTemplates.CandidateList;

    public async Task<ExportFile> GenerateExport(Guid entityId)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .IgnoreQueryFilters() // allow to export contests with a deleted DOI
            .Include(c => c.ProportionalElections)
            .Include(c => c.MajorityElections)
            .FirstOrDefaultAsync(c => c.Id == entityId)
            ?? throw new EntityNotFoundException(entityId);

        var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
        var accessibleDoiIds = doiHierarchyGroups.AccessibleDoiIds.ToHashSet();
        if (!_auth.HasPermission(Permissions.Export.ExportAllPoliticalBusinesses))
        {
            // Can only export when the tenant is involved in the contest (somewhere in the hierarchy)
            if (!accessibleDoiIds.Contains(contest.DomainOfInfluenceId))
            {
                throw new ForbiddenException();
            }

            // Can only export political businesses which are accessible.
            contest.PoliticalBusinesses = contest.PoliticalBusinesses
                .Where(pb => accessibleDoiIds.Contains(pb.DomainOfInfluenceId))
                .ToList();
        }

        var allEntries = Enumerable.Empty<CandidateListEntry>();

        var peEntries = await _proportionalElectionGenerator.BuildEntries(contest.ProportionalElections.Select(e => e.Id), false);
        allEntries = allEntries.Concat(peEntries);

        var meEntries = await _majorityElectionGenerator.BuildEntries(contest.MajorityElections.Select(e => e.Id), false);
        allEntries = allEntries.Concat(meEntries);

        var fileName = string.Format(BasisCsvContestTemplates.CandidateList.Filename, LanguageUtil.GetInCurrentLanguage(contest.Description)) + FileExtensions.Csv;
        return new StreamedExportFile(
            (writer, ct) => _csvService.Render(writer, allEntries, ct),
            fileName,
            MediaTypeNames.Text.Csv);
    }
}
