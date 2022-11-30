// (c) Copyright 2022 by Abraxas Informatik AG
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
    private readonly Ech159Serializer _ech159Serializer;
    private readonly Ech157Serializer _ech157Serializer;
    private readonly PermissionService _permissionService;

    public ContestEchExportsGenerator(
        IAuth auth,
        IDbRepository<DataContext, Contest> contestRepo,
        Ech159Serializer ech159Serializer,
        Ech157Serializer ech157Serializer,
        PermissionService permissionService)
    {
        _auth = auth;
        _contestRepo = contestRepo;
        _ech159Serializer = ech159Serializer;
        _ech157Serializer = ech157Serializer;
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
            .Include(c => c.ProportionalElections)
                .ThenInclude(pe => pe.ProportionalElectionListUnions)
            .Include(c => c.MajorityElections)
                .ThenInclude(me => me.MajorityElectionCandidates)
            .Include(c => c.MajorityElections)
                .ThenInclude(me => me.SecondaryMajorityElections)
                    .ThenInclude(se => se.Candidates)
            .Where(c => c.Id == contestId)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(contestId);

        var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
        if (!_auth.IsAdmin())
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

        var contestDesc = LanguageUtil.GetInCurrentLanguage(contest.Description);
        if (contest.Votes.Count > 0)
        {
            var ech159 = _ech159Serializer.ToEventInitialDelivery(contest, contest.Votes);
            var xmlBytes = EchSerializer.ToXml(ech159);
            yield return new ExportFile(xmlBytes, $"votes_{contestDesc}{FileExtensions.Xml}", MediaTypeNames.Application.Xml);
        }

        if (contest.ProportionalElections.Count > 0)
        {
            var ech157 = _ech157Serializer.ToDelivery(contest, contest.ProportionalElections);
            var xmlBytes = EchSerializer.ToXml(ech157);
            yield return new ExportFile(xmlBytes, $"proportional_elections_{contestDesc}{FileExtensions.Xml}", MediaTypeNames.Application.Xml);
        }

        if (contest.MajorityElections.Count > 0)
        {
            var ech157 = _ech157Serializer.ToDelivery(contest, contest.MajorityElections);
            var xmlBytes = EchSerializer.ToXml(ech157);
            yield return new ExportFile(xmlBytes, $"majority_elections_{contestDesc}{FileExtensions.Xml}", MediaTypeNames.Application.Xml);
        }
    }
}
