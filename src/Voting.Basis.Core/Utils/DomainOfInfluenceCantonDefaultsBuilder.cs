// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;

namespace Voting.Basis.Core.Utils;

public class DomainOfInfluenceCantonDefaultsBuilder
{
    private readonly CantonSettingsRepo _cantonSettingsRepo;
    private readonly DomainOfInfluenceRepo _doiRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly DataContext _dataContext;

    public DomainOfInfluenceCantonDefaultsBuilder(
        CantonSettingsRepo cantonSettingsRepo,
        DomainOfInfluenceRepo doiRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        DataContext dataContext)
    {
        _cantonSettingsRepo = cantonSettingsRepo;
        _doiRepo = doiRepo;
        _hierarchyRepo = hierarchyRepo;
        _dataContext = dataContext;
    }

    public async Task BuildForDomainOfInfluence(DomainOfInfluence domainOfInfluence)
    {
        var cantonSettings = await LoadCantonSettings(domainOfInfluence.Canton, domainOfInfluence.ParentId);
        BuildCantonDefaultsOnDomainOfInfluence(cantonSettings, domainOfInfluence);
    }

    public async Task<CantonSettings> LoadCantonSettings(DomainOfInfluenceCanton canton, Guid? parentId)
    {
        return canton != DomainOfInfluenceCanton.Unspecified
            ? await LoadCantonSettings(canton)
            : await LoadCantonSettings(parentId ?? throw new InvalidOperationException("Can only build canton settings for non root doi's or root dois with a canton set"));
    }

    public async Task RebuildForCanton(CantonSettings cantonSettings)
        => await Rebuild(cantonSettings, doi => doi.Canton == cantonSettings.Canton);

    public async Task RebuildForRootDomainOfInfluenceCantonUpdate(DomainOfInfluence rootDomainOfInfluence)
        => await Rebuild(await LoadCantonSettings(rootDomainOfInfluence.Id), doi => doi.Id == rootDomainOfInfluence.Id);

    public DomainOfInfluenceCantonDefaults BuildCantonDefaults(CantonSettings cantonSettings, DomainOfInfluenceType domainOfInfluenceType)
    {
        return new DomainOfInfluenceCantonDefaults
        {
            Canton = cantonSettings.Canton,
            ProportionalElectionMandateAlgorithms = cantonSettings.ProportionalElectionMandateAlgorithms,
            MajorityElectionAbsoluteMajorityAlgorithm = cantonSettings.MajorityElectionAbsoluteMajorityAlgorithm,
            MajorityElectionInvalidVotes = cantonSettings.MajorityElectionInvalidVotes,
            SwissAbroadVotingRight = GetSwissAbroadVotingRight(cantonSettings, domainOfInfluenceType),
            EnabledPoliticalBusinessUnionTypes = cantonSettings.EnabledPoliticalBusinessUnionTypes,
            MultipleVoteBallotsEnabled = cantonSettings.MultipleVoteBallotsEnabled,
            ProportionalElectionUseCandidateCheckDigit = cantonSettings.ProportionalElectionUseCandidateCheckDigit,
            MajorityElectionUseCandidateCheckDigit = cantonSettings.MajorityElectionUseCandidateCheckDigit,
            CreateContestOnHighestHierarchicalLevelEnabled = cantonSettings.CreateContestOnHighestHierarchicalLevelEnabled,
            InternalPlausibilisationDisabled = cantonSettings.InternalPlausibilisationDisabled,
            CandidateLocalityRequired = cantonSettings.CandidateLocalityRequired,
            CandidateOriginRequired = cantonSettings.CandidateOriginRequired,
            DomainOfInfluencePublishResultsOptionEnabled = cantonSettings.DomainOfInfluencePublishResultsOptionEnabled,
            SecondaryMajorityElectionOnSeparateBallot = cantonSettings.SecondaryMajorityElectionOnSeparateBallot,
            HideOccupationTitle = cantonSettings.HideOccupationTitle,
        };
    }

    /// <summary>
    /// Rebuild the canton settings by updating all affected domain of influences.
    /// </summary>
    /// <param name="cantonSettings">The canton settings to apply.</param>
    /// <param name="rootDoiPredicate">A predicate to filter out unaffected root domain of influences.</param>
    private async Task Rebuild(CantonSettings cantonSettings, Expression<Func<DomainOfInfluence, bool>> rootDoiPredicate)
    {
        var rootDoiIds = await _doiRepo.Query()
            .Where(x => x.ParentId == null)
            .Where(rootDoiPredicate)
            .Select(x => x.Id)
            .ToListAsync();

        if (rootDoiIds.Count == 0)
        {
            return;
        }

        var rootHierarchyEntries = await _hierarchyRepo.Query()
            .Where(x => rootDoiIds.Contains(x.DomainOfInfluenceId))
            .ToListAsync();
        var affectedDoiIds = rootHierarchyEntries
            .SelectMany(x => x.ChildIds)
            .Concat(rootDoiIds)
            .ToList();

        var trackedDois = await _doiRepo.Query()
            .AsTracking()
            .Where(doi => affectedDoiIds.Contains(doi.Id))
            .ToListAsync();

        foreach (var doi in trackedDois)
        {
            BuildCantonDefaultsOnDomainOfInfluence(cantonSettings, doi);
        }

        await _dataContext.SaveChangesAsync();
    }

    private void BuildCantonDefaultsOnDomainOfInfluence(CantonSettings cantonSettings, DomainOfInfluence domainOfInfluence)
    {
        domainOfInfluence.Canton = cantonSettings.Canton;
        domainOfInfluence.CantonDefaults = BuildCantonDefaults(cantonSettings, domainOfInfluence.Type);
    }

    private SwissAbroadVotingRight GetSwissAbroadVotingRight(CantonSettings cantonSettings, DomainOfInfluenceType doiType)
    {
        return cantonSettings.SwissAbroadVotingRightDomainOfInfluenceTypes.Contains(doiType)
            ? cantonSettings.SwissAbroadVotingRight
            : SwissAbroadVotingRight.NoRights;
    }

    private async Task<CantonSettings> LoadCantonSettings(Guid domainOfInfluenceId)
    {
        var canton = await _doiRepo.GetRootCanton(domainOfInfluenceId);
        return await LoadCantonSettings(canton);
    }

    private async Task<CantonSettings> LoadCantonSettings(DomainOfInfluenceCanton canton)
        => await _cantonSettingsRepo.GetByDomainOfInfluenceCanton(canton)
            ?? new CantonSettings { Canton = canton };
}
