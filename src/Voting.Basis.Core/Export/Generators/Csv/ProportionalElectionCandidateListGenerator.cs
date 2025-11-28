// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Export.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators.Csv;

public class ProportionalElectionCandidateListGenerator : IExportGenerator
{
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly CsvService _csvService;
    private readonly PermissionService _permissionService;

    public ProportionalElectionCandidateListGenerator(
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        CsvService csvService,
        PermissionService permissionService)
    {
        _electionRepo = electionRepo;
        _csvService = csvService;
        _permissionService = permissionService;
    }

    public TemplateModel Template => BasisCsvProportionalElectionTemplates.CandidateList;

    public async Task<ExportFile> GenerateExport(Guid entityId)
    {
        var entries = await BuildEntries([entityId], true);
        var entryList = entries.ToList();

        var fileName = string.Format(BasisCsvProportionalElectionTemplates.CandidateList.Filename, entryList.FirstOrDefault()?.ElectionName) + FileExtensions.Csv;

        return new StreamedExportFile(
            (writer, ct) => _csvService.Render(writer, entryList, ct),
            fileName,
            MediaTypeNames.Text.Csv);
    }

    internal async Task<IEnumerable<CandidateListEntry>> BuildEntries(IEnumerable<Guid> electionIds, bool needsPermissionCheck)
    {
        var proportionalElections = await _electionRepo.Query()
            .AsSplitQuery()
            .Include(pe => pe.DomainOfInfluence)
            .Include(pe => pe.ProportionalElectionLists)
                .ThenInclude(l => l.ProportionalElectionCandidates)
                .ThenInclude(c => c.Party)
            .Include(pe => pe.ProportionalElectionLists)
                .ThenInclude(l => l.ProportionalElectionListUnionEntries)
                .ThenInclude(lu => lu.ProportionalElectionListUnion)
            .Where(pe => electionIds.Contains(pe.Id))
            .ToListAsync();

        if (needsPermissionCheck)
        {
            var doiIds = proportionalElections.Select(e => e.DomainOfInfluenceId).Distinct();
            await _permissionService.EnsureIsOwnerOfDomainOfInfluencesOrHasCantonAdminPermissions(doiIds, true);
        }

        return ToCandidateListEntries(proportionalElections);
    }

    private static IEnumerable<CandidateListEntry> ToCandidateListEntries(IEnumerable<ProportionalElection> elections)
    {
        foreach (var election in elections.OrderBy(e => e.DomainOfInfluence!.Type).ThenBy(e => e.PoliticalBusinessNumber))
        {
            foreach (var list in election.ProportionalElectionLists.OrderBy(c => c.Position))
            {
                foreach (var candidate in list.ProportionalElectionCandidates.OrderBy(c => c.Position))
                {
                    yield return ToEntry(election, candidate, false);

                    // Emit accumulated candidates twice. Only the second entry should show as accumulated to make filtering easier
                    if (candidate.Accumulated)
                    {
                        yield return ToEntry(election, candidate, true);
                    }
                }
            }
        }
    }

    private static CandidateListEntry ToEntry(
        ProportionalElection election,
        ProportionalElectionCandidate candidate,
        bool accumulated)
    {
        var list = candidate.ProportionalElectionList;
        var doi = election.DomainOfInfluence!;
        return new CandidateListEntry
        {
            ElectionName = LanguageUtil.GetInCurrentLanguage(election.OfficialDescription),
            ElectionNameShort = LanguageUtil.GetInCurrentLanguage(election.ShortDescription),
            DomainOfInfluenceName = doi.Name,
            DomainOfInfluenceNameShort = doi.ShortName,
            DomainOfInfluenceNameForProtocol = doi.NameForProtocol,
            DomainOfInfluenceInternalDescription = doi.ShortName,
            PoliticalBusinessId = election.Id,
            PoliticalUnionId = election.ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElectionUnionId,
            ListNumber = list.OrderNumber,
            Number = candidate.Number,
            CheckDigit = candidate.CheckDigit,
            Position = accumulated ? candidate.AccumulatedPosition : candidate.Position,
            PoliticalLastName = candidate.PoliticalLastName,
            PoliticalFirstName = candidate.PoliticalFirstName,
            LastName = candidate.LastName,
            FirstName = candidate.FirstName,
            Country = candidate.Country,
            DateOfBirth = candidate.DateOfBirth,
            YearOfBirth = candidate.DateOfBirth?.Year,
            Origin = candidate.Origin,
            Title = candidate.Title,
            Occupation = LanguageUtil.GetInCurrentLanguage(candidate.Occupation),
            Street = $"{candidate.Street} {candidate.HouseNumber}".Trim(),
            ZipCode = candidate.ZipCode,
            Locality = candidate.Locality,
            Gender = candidate.Sex,
            Incumbent = candidate.Incumbent,
            Accumulated = accumulated,
            Party = candidate.Party == null ? null : LanguageUtil.GetInCurrentLanguage(candidate.Party.Name),
            ListDescription = LanguageUtil.GetInCurrentLanguage(list.Description),
            ListDescriptionShort = LanguageUtil.GetInCurrentLanguage(list.ShortDescription),
            ListUnionName = GetListUnionDescription(list, false),
            ListSubUnionName = GetListUnionDescription(list, true),
        };
    }

    private static string? GetListUnionDescription(ProportionalElectionList list, bool subListUnion)
    {
        var listUnion = list.ProportionalElectionListUnionEntries
            .FirstOrDefault(lu => lu.ProportionalElectionListUnion.IsSubListUnion == subListUnion);
        return listUnion == null
            ? null
            : LanguageUtil.GetInCurrentLanguage(listUnion.ProportionalElectionListUnion.Description);
    }
}
