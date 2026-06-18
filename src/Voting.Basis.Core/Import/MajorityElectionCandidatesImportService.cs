// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Lib.Eventing.Persistence;
using MajorityElectionCandidate = Voting.Basis.Core.Domain.MajorityElectionCandidate;

namespace Voting.Basis.Core.Import;

public class MajorityElectionCandidatesImportService
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ContestReader _contestReader;
    private readonly PermissionService _permissionService;
    private readonly DomainOfInfluenceReader _domainOfInfluenceReader;

    public MajorityElectionCandidatesImportService(
        IAggregateRepository aggregateRepository,
        ContestReader contestReader,
        PermissionService permissionService,
        DomainOfInfluenceReader domainOfInfluenceReader)
    {
        _aggregateRepository = aggregateRepository;
        _contestReader = contestReader;
        _permissionService = permissionService;
        _domainOfInfluenceReader = domainOfInfluenceReader;
    }

    public async Task Import(
        Guid majorityElectionId,
        IEnumerable<MajorityElectionCandidate> candidates)
    {
        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(majorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasCantonAdminPermissions(majorityElection.DomainOfInfluenceId, false);

        var doi = await _domainOfInfluenceReader.Get(majorityElection.DomainOfInfluenceId);

        var candidateValidationParams = new CandidateValidationParams(doi, true);
        var contest = await _contestReader.Get(majorityElection.ContestId);
        if (contest.TestingPhaseEnded)
        {
            throw new ContestTestingPhaseEndedException();
        }

        var candidatesToImport = candidates.ToList();
        var idVerifier = new IdVerifier();

        foreach (var existingCandidate in majorityElection.Candidates)
        {
            idVerifier.Add(existingCandidate.Id);

            var matchingCandidate = candidatesToImport.Find(c => c.PoliticalFirstName == existingCandidate.PoliticalFirstName && c.PoliticalLastName == existingCandidate.PoliticalLastName);
            if (matchingCandidate != null)
            {
                matchingCandidate.Id = existingCandidate.Id;
                matchingCandidate.MajorityElectionId = existingCandidate.MajorityElectionId;
                matchingCandidate.Position = existingCandidate.Position;
                EnsureUniqueCandidateNumber(matchingCandidate, majorityElection);
                majorityElection.UpdateCandidateFrom(matchingCandidate, candidateValidationParams);

                candidatesToImport.Remove(matchingCandidate);
            }
        }

        foreach (var candidate in candidatesToImport)
        {
            candidate.MajorityElectionId = majorityElectionId;
            candidate.Position = majorityElection.Candidates.Count + 1;
            EnsureUniqueCandidateNumber(candidate, majorityElection);
            majorityElection.CreateCandidateFrom(candidate, candidateValidationParams);

            idVerifier.EnsureUnique(candidate.Id);
        }

        await _aggregateRepository.Save(majorityElection);
    }

    private void EnsureUniqueCandidateNumber(MajorityElectionCandidate candidate, MajorityElectionAggregate aggregate, int? currentNumber = null)
    {
        var alreadyExists = aggregate.Candidates.Any(c => c.Id != candidate.Id && c.Number == candidate.Number);
        if (!alreadyExists)
        {
            return;
        }

        var newNumber = currentNumber.HasValue
            ? currentNumber.Value + 1
            : candidate.Position;
        candidate.Number = newNumber.ToString(CultureInfo.InvariantCulture);
        EnsureUniqueCandidateNumber(candidate, aggregate, newNumber);
    }
}
