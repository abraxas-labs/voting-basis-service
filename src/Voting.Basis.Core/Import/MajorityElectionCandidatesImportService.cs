// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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
        await _permissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElection.DomainOfInfluenceId);

        var doi = await _domainOfInfluenceReader.Get(majorityElection.DomainOfInfluenceId);
        var candidateValidationParams = new CandidateValidationParams(doi);
        var contest = await _contestReader.Get(majorityElection.ContestId);
        if (contest.TestingPhaseEnded)
        {
            throw new ContestTestingPhaseEndedException();
        }

        var currentCandidatePosition = majorityElection.Candidates.MaxOrDefault(c => c.Position);
        var candidatesFirstLastNameDob = majorityElection.Candidates
            .Select(x => (x.PoliticalFirstName, x.PoliticalLastName, x.DateOfBirth))
            .ToHashSet();

        var idVerifier = new IdVerifier();
        foreach (var candidate in candidates)
        {
            if (candidatesFirstLastNameDob.Add((candidate.PoliticalFirstName, candidate.PoliticalLastName, candidate.DateOfBirth)))
            {
                candidate.Position = ++currentCandidatePosition;
                candidate.MajorityElectionId = majorityElectionId;
                majorityElection.CreateCandidateFrom(candidate, candidateValidationParams);
                idVerifier.EnsureUnique(candidate.Id);
            }
        }

        await _aggregateRepository.Save(majorityElection);
    }
}
