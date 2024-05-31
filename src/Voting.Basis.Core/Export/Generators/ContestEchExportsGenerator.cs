// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Export.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Ech.Converters;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Basis;

namespace Voting.Basis.Core.Export.Generators;

public class ContestEchExportsGenerator : IExportsGenerator
{
    private readonly IAuth _auth;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly Ech0159Serializer _ech0159Serializer;
    private readonly Ech0157Serializer _ech0157Serializer;
    private readonly PermissionService _permissionService;

    public ContestEchExportsGenerator(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        Ech0159Serializer ech0159Serializer,
        Ech0157Serializer ech0157Serializer,
        PermissionService permissionService)
    {
        _auth = auth;
        _contestRepo = contestRepo;
        _ech0159Serializer = ech0159Serializer;
        _ech0157Serializer = ech0157Serializer;
        _permissionService = permissionService;
    }

    public TemplateModel Template => BasisXmlContestTemplates.Ech0157And0159;

    public async IAsyncEnumerable<ExportFile> GenerateExports(Guid contestId)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
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
            .Include(c => c.MajorityElections)
                .ThenInclude(me => me.MajorityElectionCandidates)
            .Include(c => c.MajorityElections)
                .ThenInclude(me => me.SecondaryMajorityElections)
                    .ThenInclude(se => se.Candidates)
            .Include(c => c.DomainOfInfluence)
            .Where(c => c.Id == contestId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(contestId);

        var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
        if (!_auth.HasPermission(Permissions.Export.ExportAllPoliticalBusinesses))
        {
            // Can only export when the tenant is involved in the contest (somewhere in the hierarchy)
            if (!doiHierarchyGroups.AccessibleDoiIds.Contains(contest.DomainOfInfluenceId))
            {
                throw new ForbiddenException();
            }

            // Can only export political businesses in the same or lower hierarchy
            // Cannot export political businesses from "parent domain of influences"
            contest.PoliticalBusinesses = contest.PoliticalBusinesses
                .Where(pb => doiHierarchyGroups.TenantAndChildDoiIds.Contains(pb.DomainOfInfluenceId))
                .ToList();
        }

        if (contest.Votes.Count > 0)
        {
            var xmlBytes = _ech0159Serializer.ToEventInitialDelivery(contest, contest.Votes);
            yield return new ExportFile(
                xmlBytes,
                FileNameUtil.GetXmlFileName(Ech0159Serializer.EchNumber, Ech0159Serializer.EchVersion, contest.DomainOfInfluence.Canton, contest.Date, $"votes"),
                MediaTypeNames.Application.Xml);
        }

        if (contest.ProportionalElections.Count > 0)
        {
            var xmlBytes = _ech0157Serializer.ToDelivery(contest, contest.ProportionalElections);
            yield return new ExportFile(
                xmlBytes,
                FileNameUtil.GetXmlFileName(Ech0157Serializer.EchNumber, Ech0157Serializer.EchVersion, contest.DomainOfInfluence.Canton, contest.Date, $"proportional_elections"),
                MediaTypeNames.Application.Xml);
        }

        if (contest.MajorityElections.Count > 0)
        {
            var xmlBytes = _ech0157Serializer.ToDelivery(contest, contest.MajorityElections);
            yield return new ExportFile(
                xmlBytes,
                FileNameUtil.GetXmlFileName(Ech0157Serializer.EchNumber, Ech0157Serializer.EchVersion, contest.DomainOfInfluence.Canton, contest.Date, $"majority_elections"),
                MediaTypeNames.Application.Xml);
        }
    }
}
