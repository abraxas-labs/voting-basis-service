// (c) Copyright by Abraxas Informatik AG
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

public class MajorityElectionUnionWriter : PoliticalBusinessUnionWriter<MajorityElection, MajorityElectionUnion>
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;

    public MajorityElectionUnionWriter(
        IDbRepository<DataContext, MajorityElectionUnion> repo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IAuth auth,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ContestValidationService contestValidationService,
        PermissionService permissionService)
        : base(repo, majorityElectionRepo, contestRepo, auth, contestValidationService, permissionService)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
    }

    protected override PoliticalBusinessUnionType UnionType => PoliticalBusinessUnionType.MajorityElection;

    public async Task Create(Domain.MajorityElectionUnion data)
    {
        await EnsureCanCreatePoliticalBusinessUnion(data.ContestId);

        data.SecureConnectId = Auth.Tenant.Id;

        var majorityElectionUnion = _aggregateFactory.New<MajorityElectionUnionAggregate>();
        majorityElectionUnion.CreateFrom(data);

        await _aggregateRepository.Save(majorityElectionUnion);
    }

    public async Task Update(Domain.MajorityElectionUnion data)
    {
        await EnsureCanModifyPoliticalBusinessUnion(data.Id);

        var majorityElectionUnion = await _aggregateRepository.GetById<MajorityElectionUnionAggregate>(data.Id);
        majorityElectionUnion.UpdateFrom(data);

        await _aggregateRepository.Save(majorityElectionUnion);
    }

    public async Task UpdateEntries(Guid majorityElectionUnionId, List<Guid> majorityElectionIds)
    {
        await EnsureCanModifyPoliticalBusinessUnion(majorityElectionUnionId);

        var majorityElectionUnion = await _aggregateRepository.GetById<MajorityElectionUnionAggregate>(majorityElectionUnionId);
        await EnsureValidPoliticalBusinessIds(majorityElectionUnion.ContestId, majorityElectionIds);

        majorityElectionUnion.UpdateEntries(majorityElectionIds);
        await _aggregateRepository.Save(majorityElectionUnion);
    }

    public async Task Delete(Guid id)
    {
        await EnsureCanModifyPoliticalBusinessUnion(id);

        var majorityElectionUnion = await _aggregateRepository.GetById<MajorityElectionUnionAggregate>(id);
        majorityElectionUnion.Delete();

        await _aggregateRepository.Save(majorityElectionUnion);
    }
}
