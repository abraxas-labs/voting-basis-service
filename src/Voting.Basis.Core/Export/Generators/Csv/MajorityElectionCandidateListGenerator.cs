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

public class MajorityElectionCandidateListGenerator : IExportGenerator
{
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly CsvService _csvService;
    private readonly PermissionService _permissionService;

    public MajorityElectionCandidateListGenerator(
        IDbRepository<DataContext, MajorityElection> electionRepo,
        CsvService csvService,
        PermissionService permissionService)
    {
        _electionRepo = electionRepo;
        _csvService = csvService;
        _permissionService = permissionService;
    }

    public TemplateModel Template => BasisCsvMajorityElectionTemplates.CandidateList;

    public async Task<ExportFile> GenerateExport(Guid entityId)
    {
        var entries = await BuildEntries([entityId], true);
        var entryList = entries.ToList();

        var fileName = string.Format(BasisCsvMajorityElectionTemplates.CandidateList.Filename, entryList.FirstOrDefault()?.ElectionName) + FileExtensions.Csv;

        return new StreamedExportFile(
            (writer, ct) => _csvService.Render(writer, entryList, ct),
            fileName,
            MediaTypeNames.Text.Csv);
    }

    internal async Task<IEnumerable<CandidateListEntry>> BuildEntries(IEnumerable<Guid> electionIds, bool needsPermissionCheck)
    {
        var majorityElections = await _electionRepo.Query()
            .AsSplitQuery()
            .Include(pe => pe.DomainOfInfluence)
            .Include(l => l.MajorityElectionCandidates)
            .Include(l => l.SecondaryMajorityElections)
                .ThenInclude(l => l.Candidates)
            .Where(pe => electionIds.Contains(pe.Id))
            .ToListAsync();

        if (needsPermissionCheck)
        {
            var doiIds = majorityElections.Select(e => e.DomainOfInfluenceId).Distinct();
            await _permissionService.EnsureIsOwnerOfDomainOfInfluencesOrHasCantonAdminPermissions(doiIds, true);
        }

        return ToCandidateListEntries(majorityElections);
    }

    private static IEnumerable<CandidateListEntry> ToCandidateListEntries(IEnumerable<MajorityElection> elections)
    {
        foreach (var election in elections.OrderBy(e => e.DomainOfInfluence!.Type).ThenBy(e => e.PoliticalBusinessNumber))
        {
            foreach (var candidate in election.MajorityElectionCandidates.OrderBy(c => c.Position))
            {
                yield return ToEntry(election.OfficialDescription, election.ShortDescription, election.Id, election, candidate);
            }

            foreach (var secondaryElection in election.SecondaryMajorityElections.OrderBy(e => e.PoliticalBusinessNumber))
            {
                foreach (var candidate in secondaryElection.Candidates.OrderBy(c => c.Position))
                {
                    yield return ToEntry(
                        secondaryElection.OfficialDescription,
                        secondaryElection.ShortDescription,
                        secondaryElection.Id,
                        election,
                        candidate);
                }
            }
        }
    }

    private static CandidateListEntry ToEntry(
        Dictionary<string, string> electionOfficialDescription,
        Dictionary<string, string> electionShortDescription,
        Guid electionId,
        MajorityElection election,
        MajorityElectionCandidateBase candidate)
    {
        var doi = election.DomainOfInfluence!;
        return new CandidateListEntry
        {
            ElectionName = LanguageUtil.GetInCurrentLanguage(electionOfficialDescription),
            ElectionNameShort = LanguageUtil.GetInCurrentLanguage(electionShortDescription),
            DomainOfInfluenceName = doi.Name,
            DomainOfInfluenceNameShort = doi.ShortName,
            DomainOfInfluenceNameForProtocol = doi.NameForProtocol,
            DomainOfInfluenceInternalDescription = doi.ShortName,
            PoliticalBusinessId = electionId,
            PoliticalUnionId = election.MajorityElectionUnionEntries.FirstOrDefault()?.MajorityElectionUnionId,
            Number = candidate.Number,
            CheckDigit = candidate.CheckDigit,
            Position = candidate.Position,
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
            Party = LanguageUtil.GetInCurrentLanguage(candidate.PartyShortDescription),
        };
    }
}
