// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data.Models;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using ProportionalElectionCandidate = Voting.Basis.Core.Domain.ProportionalElectionCandidate;
using ProportionalElectionListUnion = Voting.Basis.Core.Domain.ProportionalElectionListUnion;

namespace Voting.Basis.Core.Import;

public class ProportionalElectionListsAndCandidatesImportService
{
    private readonly IAuth _auth;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ContestValidationService _contestValidationService;
    private readonly PermissionService _permissionService;
    private readonly DomainOfInfluenceReader _domainOfInfluenceReader;

    public ProportionalElectionListsAndCandidatesImportService(
        IAuth auth,
        IAggregateRepository aggregateRepository,
        ContestValidationService contestValidationService,
        PermissionService permissionService,
        DomainOfInfluenceReader domainOfInfluenceReader)
    {
        _auth = auth;
        _aggregateRepository = aggregateRepository;
        _contestValidationService = contestValidationService;
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
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        var listIdsToDelete = proportionalElection.Lists
            .Where(l =>
                listImports.Any(i => l.OrderNumber == i.List.OrderNumber || HasSameTranslations(l.ShortDescription, i.List.ShortDescription)))
            .Select(l => l.Id)
            .ToHashSet();
        DeleteListUnions(proportionalElection, listIdsToDelete);
        DeleteLists(proportionalElection, listIdsToDelete);

        await ImportLists(listImports, proportionalElection);
        ImportListUnions(listUnions, proportionalElection);

        await _aggregateRepository.Save(proportionalElection);
    }

    private void DeleteLists(ProportionalElectionAggregate proportionalElection, IEnumerable<Guid> listIds)
    {
        foreach (var listId in listIds)
        {
            proportionalElection.DeleteList(listId);
        }
    }

    private void DeleteListUnions(ProportionalElectionAggregate proportionalElection, HashSet<Guid> listIds)
    {
        var listUnionIdsToDelete = proportionalElection.ListUnions
            .Where(lu => !lu.IsSubListUnion && lu.ProportionalElectionListIds.Any(listIds.Contains))
            .Select(lu => lu.Id)
            .ToHashSet();

        foreach (var id in listUnionIdsToDelete)
        {
            proportionalElection.DeleteListUnion(id);
        }
    }

    private async Task ImportLists(
        IEnumerable<ProportionalElectionListImport> listImports,
        ProportionalElectionAggregate proportionalElection)
    {
        var doi = await _domainOfInfluenceReader.Get(proportionalElection.DomainOfInfluenceId);
        var currentListPosition = proportionalElection.Lists.MaxOrDefault(l => l.Position);

        foreach (var listImport in listImports)
        {
            var list = listImport.List;
            list.ProportionalElectionId = proportionalElection.Id;
            list.Position = ++currentListPosition;

            proportionalElection.CreateListFrom(list);
            ImportCandidates(listImport.Candidates, proportionalElection, list.Id, doi.Type);
        }
    }

    private void ImportCandidates(
        IReadOnlyCollection<ProportionalElectionCandidate> candidates,
        ProportionalElectionAggregate proportionalElection,
        Guid listId,
        DomainOfInfluenceType doiType)
    {
        var currentCandidatePosition = 0;

        foreach (var candidate in candidates)
        {
            candidate.ProportionalElectionListId = listId;
            candidate.Position = ++currentCandidatePosition;

            if (candidate.Accumulated)
            {
                candidate.AccumulatedPosition = ++currentCandidatePosition;
            }

            proportionalElection.CreateCandidateFrom(candidate, doiType);
        }
    }

    private void ImportListUnions(IEnumerable<ProportionalElectionListUnion> listUnions, ProportionalElectionAggregate proportionalElection)
    {
        var currentListUnionPosition = proportionalElection.ListUnions.MaxOrDefault(l => l.Position);

        foreach (var listUnion in listUnions)
        {
            var listUnionProto = new ProportionalElectionListUnion
            {
                Id = listUnion.Id,
                Description = listUnion.Description,
                Position = ++currentListUnionPosition,
                ProportionalElectionId = proportionalElection.Id,
                ProportionalElectionRootListUnionId = listUnion.ProportionalElectionRootListUnionId,
            };

            proportionalElection.CreateListUnionFrom(listUnionProto);

            var entries = new ProportionalElectionListUnionEntries
            {
                ProportionalElectionListUnionId = listUnionProto.Id,
            };

            entries.ProportionalElectionListIds.AddRange(listUnion.ProportionalElectionListIds);
            proportionalElection.UpdateListUnionEntriesFrom(entries);
        }
    }

    private bool HasSameTranslations(IDictionary<string, string> dict1, IDictionary<string, string> dict2)
    {
        foreach (var (lang, translation1) in dict1)
        {
            if (dict2.TryGetValue(lang, out var translation2)
                && !string.IsNullOrEmpty(translation2)
                && translation1 == translation2)
            {
                return true;
            }
        }

        return false;
    }
}
