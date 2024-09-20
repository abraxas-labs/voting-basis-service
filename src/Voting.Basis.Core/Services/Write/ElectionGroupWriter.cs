// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Validation;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Basis.Core.Services.Write;

public class ElectionGroupWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly PermissionService _permissionService;
    private readonly ContestValidationService _contestValidationService;

    public ElectionGroupWriter(IAggregateRepository aggregateRepository, PermissionService permissionService, ContestValidationService contestValidationService)
    {
        _aggregateRepository = aggregateRepository;
        _permissionService = permissionService;
        _contestValidationService = contestValidationService;
    }

    public async Task Update(Guid majorityElectionId, string description)
    {
        // since currently only majority elections are supported as primary elections, we can fetch the aggregate directly
        var primaryMajorityElection = await _aggregateRepository.GetById<MajorityElectionAggregate>(majorityElectionId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(primaryMajorityElection.DomainOfInfluenceId);
        await _contestValidationService.EnsureInTestingPhase(primaryMajorityElection.ContestId);

        primaryMajorityElection.UpdateElectionGroupDescription(description);
        await _aggregateRepository.Save(primaryMajorityElection);
    }
}
