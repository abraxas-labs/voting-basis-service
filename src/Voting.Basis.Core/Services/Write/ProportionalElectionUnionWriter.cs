// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Write;

public class ProportionalElectionUnionWriter : PoliticalBusinessUnionWriter<ProportionalElection, ProportionalElectionUnion>
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;

    public ProportionalElectionUnionWriter(
        IDbRepository<DataContext, ProportionalElectionUnion> repo,
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IAuth auth,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ContestValidationService contestValidationService,
        PermissionService permissionService)
        : base(repo, proportionalElectionRepo, contestRepo, auth, contestValidationService, permissionService)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
    }

    protected override PoliticalBusinessUnionType UnionType => PoliticalBusinessUnionType.ProportionalElection;

    public async Task Create(Domain.ProportionalElectionUnion data)
    {
        await EnsureCanCreatePoliticalBusinessUnion(data.ContestId);
        data.SecureConnectId = Auth.Tenant.Id;

        var proportionalElectionUnion = _aggregateFactory.New<ProportionalElectionUnionAggregate>();
        proportionalElectionUnion.CreateFrom(data);

        await _aggregateRepository.Save(proportionalElectionUnion);
    }

    public async Task Update(Domain.ProportionalElectionUnion data)
    {
        await EnsureCanModifyPoliticalBusinessUnion(data.Id);
        data.SecureConnectId = Auth.Tenant.Id;

        var proportionalElectionUnion = await _aggregateRepository.GetById<ProportionalElectionUnionAggregate>(data.Id);
        proportionalElectionUnion.UpdateFrom(data);

        await _aggregateRepository.Save(proportionalElectionUnion);
    }

    public async Task UpdateEntries(Guid proportionalElectionUnionId, List<Guid> proportionalElectionIds)
    {
        await EnsureCanModifyPoliticalBusinessUnion(proportionalElectionUnionId);

        var proportionalElectionUnion = await _aggregateRepository.GetById<ProportionalElectionUnionAggregate>(proportionalElectionUnionId);
        await EnsureValidPoliticalBusinessIds(proportionalElectionUnion.ContestId, proportionalElectionIds);

        proportionalElectionUnion.UpdateEntries(proportionalElectionIds);
        await _aggregateRepository.Save(proportionalElectionUnion);
    }

    public async Task Delete(Guid proportionalElectionUnionId)
    {
        await EnsureCanModifyPoliticalBusinessUnion(proportionalElectionUnionId);

        var proportionalElectionUnion = await _aggregateRepository.GetById<ProportionalElectionUnionAggregate>(proportionalElectionUnionId);
        proportionalElectionUnion.Delete();

        await _aggregateRepository.Save(proportionalElectionUnion);
    }
}
