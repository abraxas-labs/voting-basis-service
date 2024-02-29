// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly DataContext _dataContext;

    public DomainOfInfluenceCantonDefaultsBuilder(
        CantonSettingsRepo cantonSettingsRepo,
        DomainOfInfluenceRepo doiRepo,
        DataContext dataContext)
    {
        _cantonSettingsRepo = cantonSettingsRepo;
        _doiRepo = doiRepo;
        _dataContext = dataContext;
    }

    public async Task BuildForDomainOfInfluence(DomainOfInfluence domainOfInfluence)
    {
        var cantonSettings = domainOfInfluence.Canton != DomainOfInfluenceCanton.Unspecified
            ? await LoadCantonSettings(domainOfInfluence.Canton)
            : await LoadCantonSettings(domainOfInfluence.ParentId ?? throw new InvalidOperationException("Can only build canton settings for non root doi's or root dois with a canton set"));
        BuildCantonDefaultsOnDomainOfInfluence(cantonSettings, domainOfInfluence);
    }

    public async Task RebuildForCanton(CantonSettings cantonSettings)
        => await Rebuild(cantonSettings, await _doiRepo.Query().ToListAsync(), doi => doi.Canton == cantonSettings.Canton);

    public async Task RebuildForRootDomainOfInfluenceCantonUpdate(DomainOfInfluence rootDomainOfInfluence, List<DomainOfInfluence> allDomainOfInfluences)
         => await Rebuild(await LoadCantonSettings(rootDomainOfInfluence.Id), allDomainOfInfluences, doi => doi.Id == rootDomainOfInfluence.Id);

    /// <summary>
    /// Rebuild the canton settings by updating all affected domain of influences.
    /// </summary>
    /// <param name="cantonSettings">The canton settings to apply.</param>
    /// <param name="allDomainOfInfluences">All domain of influences that exist.</param>
    /// <param name="rootDoiPredicate">A predicate to filter out unaffected root domain of influences.</param>
    private async Task Rebuild(
        CantonSettings cantonSettings,
        List<DomainOfInfluence> allDomainOfInfluences,
        Func<DomainOfInfluence, bool> rootDoiPredicate)
    {
        var tree = DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences);

        // Filter out the root domain of influences that are not affected
        tree = tree.Where(rootDoiPredicate).ToList();

        // Collect the root domain of influence and all its children
        var affectedDois = tree.Flatten(doi => doi.Children).ToList();
        var affectedDoiIds = affectedDois.ConvertAll(doi => doi.Id);

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
        domainOfInfluence.CantonDefaults = new DomainOfInfluenceCantonDefaults
        {
            Canton = cantonSettings.Canton,
            ProportionalElectionMandateAlgorithms = cantonSettings.ProportionalElectionMandateAlgorithms,
            MajorityElectionAbsoluteMajorityAlgorithm = cantonSettings.MajorityElectionAbsoluteMajorityAlgorithm,
            MajorityElectionInvalidVotes = cantonSettings.MajorityElectionInvalidVotes,
            SwissAbroadVotingRight = GetSwissAbroadVotingRight(cantonSettings, domainOfInfluence.Type),
            EnabledPoliticalBusinessUnionTypes = cantonSettings.EnabledPoliticalBusinessUnionTypes,
            MultipleVoteBallotsEnabled = cantonSettings.MultipleVoteBallotsEnabled,
            ProportionalElectionUseCandidateCheckDigit = cantonSettings.ProportionalElectionUseCandidateCheckDigit,
            MajorityElectionUseCandidateCheckDigit = cantonSettings.MajorityElectionUseCandidateCheckDigit,
        };
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
