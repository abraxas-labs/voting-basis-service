// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Validation;
using Voting.Lib.Eventing.Persistence;
using ProportionalElectionCandidate = Voting.Basis.Core.Domain.ProportionalElectionCandidate;
using ProportionalElectionList = Voting.Basis.Core.Domain.ProportionalElectionList;
using ProportionalElectionListUnion = Voting.Basis.Core.Domain.ProportionalElectionListUnion;

namespace Voting.Basis.Core.Import;

public class ProportionalElectionListsAndCandidatesImportService
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ContestValidationService _contestValidationService;
    private readonly PermissionService _permissionService;
    private readonly DomainOfInfluenceReader _domainOfInfluenceReader;

    public ProportionalElectionListsAndCandidatesImportService(
        IAggregateRepository aggregateRepository,
        ContestValidationService contestValidationService,
        PermissionService permissionService,
        DomainOfInfluenceReader domainOfInfluenceReader)
    {
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
        var proportionalElection = await _aggregateRepository.GetById<ProportionalElectionAggregate>(proportionalElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(proportionalElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(proportionalElection.ContestId);

        var partyIds = await _domainOfInfluenceReader.GetPartyIds(proportionalElection.DomainOfInfluenceId);

        var listIdsToDelete = proportionalElection.Lists
            .Where(l => listImports.Any(i => l.OrderNumber == i.List.OrderNumber))
            .Select(l => l.Id)
            .ToHashSet();
        DeleteListUnions(proportionalElection, listIdsToDelete);
        DeleteLists(proportionalElection, listIdsToDelete);

        var idVerifier = new IdVerifier();
        await ImportLists(listImports, proportionalElection, partyIds, idVerifier);
        ImportListUnions(listUnions, proportionalElection, idVerifier);

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
        ProportionalElectionAggregate proportionalElection,
        IReadOnlySet<Guid> partyIds,
        IdVerifier idVerifier)
    {
        var doi = await _domainOfInfluenceReader.Get(proportionalElection.DomainOfInfluenceId);
        var currentListPosition = proportionalElection.Lists.MaxOrDefault(l => l.Position);

        foreach (var listImport in listImports)
        {
            var list = listImport.List;
            list.ProportionalElectionId = proportionalElection.Id;
            list.Position = ++currentListPosition;

            proportionalElection.CreateListFrom(list);
            idVerifier.EnsureUnique(list.Id);
            ImportCandidates(listImport.Candidates, proportionalElection, list, doi, partyIds, idVerifier);
        }
    }

    private void ImportCandidates(
        IReadOnlyCollection<ProportionalElectionCandidate> candidates,
        ProportionalElectionAggregate proportionalElection,
        ProportionalElectionList list,
        Data.Models.DomainOfInfluence doi,
        IReadOnlySet<Guid> partyIds,
        IdVerifier idVerifier)
    {
        var candidateValidationParams = new CandidateValidationParams(doi);

        foreach (var candidate in candidates)
        {
            if (candidate.PartyId.HasValue && !partyIds.Contains(candidate.PartyId.Value))
            {
                throw new ValidationException($"Party with id {candidate.PartyId} referenced by candidate {list.OrderNumber}/{candidate.Number} not found");
            }

            candidate.ProportionalElectionListId = list.Id;
            proportionalElection.CreateCandidateFrom(candidate, candidateValidationParams);
            idVerifier.EnsureUnique(candidate.Id);
        }
    }

    private void ImportListUnions(IEnumerable<ProportionalElectionListUnion> listUnions, ProportionalElectionAggregate proportionalElection, IdVerifier idVerifier)
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
            idVerifier.EnsureUnique(listUnionProto.Id);

            var entries = new ProportionalElectionListUnionEntries
            {
                ProportionalElectionListUnionId = listUnionProto.Id,
            };

            entries.ProportionalElectionListIds.AddRange(listUnion.ProportionalElectionListIds);
            proportionalElection.UpdateListUnionEntriesFrom(entries);
        }
    }
}
