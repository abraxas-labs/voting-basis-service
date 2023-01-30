// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Import;

public class ProportionalElectionListsAndCandidatesImportService
{
    private readonly IAuth _auth;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ContestReader _contestReader;
    private readonly PermissionService _permissionService;
    private readonly DomainOfInfluenceReader _domainOfInfluenceReader;

    public ProportionalElectionListsAndCandidatesImportService(
        IAuth auth,
        IAggregateRepository aggregateRepository,
        ContestReader contestReader,
        PermissionService permissionService,
        DomainOfInfluenceReader domainOfInfluenceReader)
    {
        _auth = auth;
        _aggregateRepository = aggregateRepository;
        _contestReader = contestReader;
        _permissionService = permissionService;
        _domainOfInfluenceReader = domainOfInfluenceReader;
    }

    public async Task Import(
        Guid proportionalElectionId,
        IReadOnlyCollection<ProportionalElectionListImport> listImports,
        IReadOnlyCollection<ProportionalElectionListUnion> listUnions)
    {
        _auth.EnsureAdminOrElectionAdmin();

        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(proportionalElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(proportionalElection.DomainOfInfluenceId);

        var contest = await _contestReader.Get(proportionalElection.ContestId);
        if (contest.TestingPhaseEnded)
        {
            throw new ContestTestingPhaseEndedException();
        }

        // needs mapping because if lists or listUnions already exist then they will have a different id.
        var existingListByIncomingListId = listImports
            .Select(x => (x.List.Id, Item: GetMatchingItem(proportionalElection.Lists, l => l.ShortDescription, x.List.ShortDescription)))
            .Where(x => x.Item != null)
            .ToDictionary(x => x.Id, x => x.Item!);

        var existingListUnionByIncomingListUnionId = listUnions
            .Select(x => (x.Id, Item: GetMatchingItem(proportionalElection.ListUnions, l => l.Description, x.Description)))
            .Where(x => x.Item != null)
            .ToDictionary(x => x.Id, x => x.Item!);

        await ImportLists(listImports, proportionalElection, existingListByIncomingListId);
        ImportListUnions(listUnions, proportionalElection, existingListByIncomingListId, existingListUnionByIncomingListUnionId);

        await _aggregateRepository.Save(proportionalElection);
    }

    private async Task ImportLists(
        IEnumerable<ProportionalElectionListImport> listImports,
        ProportionalElectionAggregate proportionalElection,
        IReadOnlyDictionary<Guid, ProportionalElectionList> existingListByIncomingListId)
    {
        var currentListPosition = proportionalElection.Lists.MaxOrDefault(l => l.Position);

        foreach (var listImport in listImports)
        {
            var list = listImport.List;
            list.ProportionalElectionId = proportionalElection.Id;

            if (!existingListByIncomingListId.TryGetValue(list.Id, out var existingList))
            {
                list.Position = ++currentListPosition;
                proportionalElection.CreateListFrom(list);
            }

            var listId = existingList?.Id ?? list.Id;
            var existingCandidates = existingList?.Candidates ?? new List<ProportionalElectionCandidate>();

            await ImportCandidates(listImport.Candidates, existingCandidates, proportionalElection, listId);
        }
    }

    private async Task ImportCandidates(
        IReadOnlyCollection<ProportionalElectionCandidate> candidates,
        IReadOnlyCollection<ProportionalElectionCandidate> existingCandidates,
        ProportionalElectionAggregate proportionalElection,
        Guid listId)
    {
        var currentCandidatePosition = existingCandidates.MaxOrDefault(c => c.Position);
        var doi = await _domainOfInfluenceReader.Get(proportionalElection.DomainOfInfluenceId);

        foreach (var candidate in candidates)
        {
            var existingCandidate = existingCandidates.FirstOrDefault(c =>
                c.PoliticalFirstName == candidate.PoliticalFirstName
                && c.PoliticalLastName == candidate.PoliticalLastName
                && c.DateOfBirth == candidate.DateOfBirth);

            if (existingCandidate == null)
            {
                candidate.ProportionalElectionListId = listId;
                candidate.Position = ++currentCandidatePosition;

                if (candidate.Accumulated)
                {
                    candidate.AccumulatedPosition = ++currentCandidatePosition;
                }

                proportionalElection.CreateCandidateFrom(candidate, doi.Type);
            }
        }
    }

    private void ImportListUnions(
        IEnumerable<ProportionalElectionListUnion> listUnions,
        ProportionalElectionAggregate proportionalElection,
        IReadOnlyDictionary<Guid, ProportionalElectionList> existingListByIncomingListId,
        IReadOnlyDictionary<Guid, ProportionalElectionListUnion> existingListUnionByIncomingListUnionId)
    {
        var currentListUnionPosition = proportionalElection.ListUnions.MaxOrDefault(l => l.Position);

        foreach (var listUnion in listUnions)
        {
            if (existingListUnionByIncomingListUnionId.ContainsKey(listUnion.Id))
            {
                continue;
            }

            existingListUnionByIncomingListUnionId.TryGetValue(listUnion.ProportionalElectionRootListUnionId ?? Guid.Empty, out var rootListUnion);
            var rootListUnionId = rootListUnion?.Id ?? listUnion.ProportionalElectionRootListUnionId;

            var listUnionProto = new ProportionalElectionListUnion
            {
                Id = listUnion.Id,
                Description = listUnion.Description,
                Position = ++currentListUnionPosition,
                ProportionalElectionId = proportionalElection.Id,
                ProportionalElectionRootListUnionId = rootListUnionId,
            };

            proportionalElection.CreateListUnionFrom(listUnionProto);

            var entries = new ProportionalElectionListUnionEntries
            {
                ProportionalElectionListUnionId = listUnionProto.Id,
            };

            var listIds = listUnion.ProportionalElectionListIds
                .ConvertAll(listId => existingListByIncomingListId.GetValueOrDefault(listId)?.Id ?? listId)
;

            entries.ProportionalElectionListIds.AddRange(listIds);
            proportionalElection.UpdateListUnionEntriesFrom(entries);
        }
    }

    private T? GetMatchingItem<T>(IEnumerable<T> existingItems, Func<T, IDictionary<string, string>> translationSelector, IDictionary<string, string> incomingTranslationDict)
        where T : class
    {
        if (!existingItems.Any() || incomingTranslationDict.Count == 0)
        {
            return null;
        }

        foreach (var item in existingItems)
        {
            var existingTranslationDict = translationSelector(item);

            foreach (var (lang, existingTranslation) in existingTranslationDict)
            {
                // if at least one language has the same translation, it will match.
                if (incomingTranslationDict.TryGetValue(lang, out var incomingTranslation)
                    && !string.IsNullOrEmpty(incomingTranslation)
                    && existingTranslation == incomingTranslation)
                {
                    return item;
                }
            }
        }

        return null;
    }
}
