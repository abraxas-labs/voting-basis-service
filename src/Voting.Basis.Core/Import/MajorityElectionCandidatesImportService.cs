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

public class MajorityElectionCandidatesImportService
{
    private readonly IAuth _auth;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ContestReader _contestReader;
    private readonly PermissionService _permissionService;

    public MajorityElectionCandidatesImportService(
        IAuth auth,
        IAggregateRepository aggregateRepository,
        ContestReader contestReader,
        PermissionService permissionService)
    {
        _auth = auth;
        _aggregateRepository = aggregateRepository;
        _contestReader = contestReader;
        _permissionService = permissionService;
    }

    public async Task Import(
        Guid majorityElectionId,
        IEnumerable<MajorityElectionCandidate> candidates)
    {
        _auth.EnsureAdminOrElectionAdmin();

        var majorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(majorityElectionId);
        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(majorityElection.DomainOfInfluenceId);

        var contest = await _contestReader.Get(majorityElection.ContestId);
        if (contest.TestingPhaseEnded)
        {
            throw new ContestTestingPhaseEndedException();
        }

        var currentCandidatePosition = majorityElection.Candidates.MaxOrDefault(c => c.Position);
        var candidatesFirstLastNameDob = majorityElection.Candidates
            .Select(x => (x.PoliticalFirstName, x.PoliticalLastName, x.DateOfBirth))
            .ToHashSet();

        foreach (var candidate in candidates)
        {
            if (candidatesFirstLastNameDob.Add((candidate.PoliticalFirstName, candidate.PoliticalLastName, candidate.DateOfBirth)))
            {
                candidate.Position = ++currentCandidatePosition;
                candidate.MajorityElectionId = majorityElectionId;
                majorityElection.CreateCandidateFrom(candidate);
            }
        }

        await _aggregateRepository.Save(majorityElection);
    }
}
