// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Export.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;

namespace Voting.Basis.Core.Export.Generators;

public abstract class ContestEchExportsGeneratorBase : IExportsGenerator
{
    private readonly IAuth _auth;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly EchSerializerProvider _echSerializerProvider;
    private readonly PermissionService _permissionService;
    private readonly CountingCircleReader _countingCircleReader;

    public ContestEchExportsGeneratorBase(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        EchSerializerProvider echSerializerProvider,
        PermissionService permissionService,
        CountingCircleReader countingCircleReader)
    {
        _auth = auth;
        _contestRepo = contestRepo;
        _echSerializerProvider = echSerializerProvider;
        _permissionService = permissionService;
        _countingCircleReader = countingCircleReader;
    }

    public abstract TemplateModel Template { get; }

    public async IAsyncEnumerable<ExportFile> GenerateExports(Guid contestId)
    {
        var contest = await FetchContestData(contestId);

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

        var eCounting = await _countingCircleReader.OwnsAnyECountingCountingCircle();
        var ech0157Serializer = _echSerializerProvider.GetEch0157Serializer(eCounting);
        var ech0159Serializer = _echSerializerProvider.GetEch0159Serializer(eCounting);

        if (contest.Votes.Count > 0)
        {
            var xmlBytes = await ech0159Serializer.ToEventInitialDelivery(contest, contest.Votes);

            yield return new ExportFile(
                xmlBytes,
                FileNameUtil.GetXmlFileName(
                    ech0159Serializer.EchNumber,
                    ech0159Serializer.EchVersion,
                    contest.DomainOfInfluence.Canton,
                    contest.Date,
                    "votes"),
                MediaTypeNames.Application.Xml);
        }

        if (contest.ProportionalElections.Count > 0)
        {
            var xmlBytes = await ech0157Serializer.ToDelivery(contest, contest.ProportionalElections);

            yield return new ExportFile(
                xmlBytes,
                FileNameUtil.GetXmlFileName(
                    ech0157Serializer.EchNumber,
                    ech0157Serializer.EchVersion,
                    contest.DomainOfInfluence.Canton,
                    contest.Date,
                    "proportional_elections"),
                MediaTypeNames.Application.Xml);
        }

        if (contest.MajorityElections.Count > 0)
        {
            var xmlBytes = await ech0157Serializer.ToDelivery(contest, contest.MajorityElections);

            yield return new ExportFile(
                xmlBytes,
                FileNameUtil.GetXmlFileName(
                    ech0157Serializer.EchNumber,
                    ech0157Serializer.EchVersion,
                    contest.DomainOfInfluence.Canton,
                    contest.Date,
                    "majority_elections"),
                MediaTypeNames.Application.Xml);
        }
    }

    protected virtual IQueryable<Contest> FilterPoliticalBusinesses(IQueryable<Contest> baseQuery)
        => baseQuery;

    private async Task<Contest> FetchContestData(Guid contestId)
    {
        var query = _contestRepo.Query()
            .AsSplitQuery()
            .IgnoreQueryFilters(); // allow to export contests with a deleted DOI
        return await FilterPoliticalBusinesses(query)
            .Include(c => c.Votes)
                .ThenInclude(v => v.Ballots)
                    .ThenInclude(b => b.BallotQuestions)
            .Include(c => c.Votes)
                .ThenInclude(v => v.Ballots)
                    .ThenInclude(b => b.TieBreakQuestions)
            .Include(c => c.Votes)
                .ThenInclude(v => v.DomainOfInfluence)
            .Include(c => c.ProportionalElections)
                .ThenInclude(pe => pe.ProportionalElectionLists)
                    .ThenInclude(l => l.ProportionalElectionCandidates)
                        .ThenInclude(c => c.Party)
            .Include(c => c.ProportionalElections)
                .ThenInclude(pe => pe.ProportionalElectionListUnions)
            .Include(c => c.ProportionalElections)
                .ThenInclude(pe => pe.DomainOfInfluence)
            .Include(c => c.MajorityElections)
                .ThenInclude(me => me.MajorityElectionCandidates)
            .Include(c => c.MajorityElections)
                .ThenInclude(me => me.SecondaryMajorityElections)
                    .ThenInclude(se => se.Candidates)
            .Include(c => c.MajorityElections)
                .ThenInclude(me => me.DomainOfInfluence)
            .Include(c => c.DomainOfInfluence)
            .Where(c => c.Id == contestId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(contestId);
    }
}
