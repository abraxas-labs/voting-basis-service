// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Services;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Write;

public class CountingCircleWriter
{
    private readonly IDbRepository<DataContext, CountingCircle> _repo;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAuth _auth;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;
    private readonly PermissionService _permissionService;

    public CountingCircleWriter(
        IDbRepository<DataContext, CountingCircle> repo,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IAuth auth,
        ITenantService tenantService,
        IMapper mapper,
        PermissionService permissionService)
    {
        _repo = repo;
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _auth = auth;
        _tenantService = tenantService;
        _mapper = mapper;
        _permissionService = permissionService;
    }

    public async Task Create(Domain.CountingCircle data)
    {
        await SetResponsibleAuthorityTenant(data);
        await EnsureCanCreate(data);

        var countingCircle = _aggregateFactory.New<CountingCircleAggregate>();
        countingCircle.CreateFrom(data);

        await _aggregateRepository.Save(countingCircle);
    }

    public async Task Update(Domain.CountingCircle data)
    {
        await SetResponsibleAuthorityTenant(data);
        await EnsureNotInScheduledMerge(data.Id);

        var countingCircle = await _aggregateRepository.GetById<CountingCircleAggregate>(data.Id);

        await EnsureCanEdit(countingCircle);
        var canUpdateAllFields = _auth.HasPermission(Permissions.CountingCircle.UpdateSameCanton);
        countingCircle.UpdateFrom(data, canUpdateAllFields);

        await _aggregateRepository.Save(countingCircle);
    }

    public async Task Delete(Guid countingCircleId)
    {
        await EnsureNotInScheduledMerge(countingCircleId);
        var countingCircle = await _aggregateRepository.GetById<CountingCircleAggregate>(countingCircleId);
        await EnsureCanDelete(countingCircle);
        countingCircle.Delete();

        await _aggregateRepository.Save(countingCircle);
    }

    public async Task<Guid> ScheduleMerge(Domain.CountingCirclesMerger data)
    {
        await ValidateAndPrepareMerger(data);
        await EnsureCanMerge(data.NewCountingCircle.Canton);

        var countingCircle = _aggregateFactory.New<CountingCircleAggregate>();
        countingCircle.ScheduleMergeFrom(data);
        await _aggregateRepository.Save(countingCircle);

        await TryActivateMergeIfOverdue(countingCircle);

        return countingCircle.Id;
    }

    public async Task UpdateScheduledMerger(Guid newCountingCircleId, Domain.CountingCirclesMerger data)
    {
        var aggregate = await _aggregateRepository.GetById<CountingCircleAggregate>(newCountingCircleId);

        await ValidateAndPrepareMerger(data, aggregate.MergerOrigin?.Id ?? throw new ValidationException("new counting circle id is immutable"));
        await EnsureCanMerge(data.NewCountingCircle.Canton);

        aggregate.UpdateScheduledMergerFrom(data);
        await _aggregateRepository.Save(aggregate);

        await TryActivateMergeIfOverdue(aggregate);
    }

    public async Task DeleteScheduledMerger(Guid newCountingCircleId)
    {
        var aggregate = await _aggregateRepository.GetById<CountingCircleAggregate>(newCountingCircleId);
        await EnsureCanMerge(aggregate.Canton);
        aggregate.CancelMerger();
        await _aggregateRepository.Save(aggregate);
    }

    internal async Task<bool> TryActivateMerge(Guid newCountingCircleId)
    {
        var countingCircle = await _aggregateRepository.GetById<CountingCircleAggregate>(newCountingCircleId);
        return await TryActivateMergeIfOverdue(countingCircle);
    }

    internal async Task<bool> TryActivateEVoting(Guid countingCircleId)
    {
        var countingCircle = await _aggregateRepository.GetById<CountingCircleAggregate>(countingCircleId);
        return await TryActivateEVotingIfOverdue(countingCircle);
    }

    private async Task SetResponsibleAuthorityTenant(Domain.CountingCircle data)
    {
        if (string.IsNullOrEmpty(data.ResponsibleAuthority?.SecureConnectId))
        {
            return;
        }

        var tenant = await _tenantService.GetTenant(data.ResponsibleAuthority.SecureConnectId, true)
                     ?? throw new ValidationException(
                         $"tenant with id {data.ResponsibleAuthority.SecureConnectId} not found");
        data.ResponsibleAuthority.Name = tenant.Name;
    }

    private async Task ValidateAndPrepareMerger(Domain.CountingCirclesMerger data, Guid? id = null)
    {
        await SetResponsibleAuthorityTenant(data.NewCountingCircle);

        var ccIds = data.MergedCountingCircleIds.ToHashSet();

        var existingCcs = await _repo.Query()
            .Where(cc => ccIds.Contains(cc.Id))
            .Select(cc => new { cc.Id, cc.MergeTarget })
            .ToListAsync();

        if (existingCcs.Count != ccIds.Count)
        {
            throw new ValidationException("Some counting circle ids to merge do not exist or are duplicates");
        }

        if (existingCcs.Any(cc => cc.MergeTarget?.Merged == false && cc.MergeTarget.Id != id))
        {
            throw new CountingCirclesInScheduledMergeException();
        }

        if (!ccIds.Contains(data.CopyFromCountingCircleId))
        {
            throw new ValidationException("Copy from counting circle needs to be in the merger counting circles");
        }

        var copyFromCcAggregate = await _aggregateRepository.GetById<CountingCircleAggregate>(data.CopyFromCountingCircleId)
            ?? throw new ValidationException("Copy from counting circle not found");

        var copyFromCc = _mapper.Map<Domain.CountingCircle>(copyFromCcAggregate);

        // fill remaining fields which are not filled in the ui by the copy from cc.
        data.NewCountingCircle.ContactPersonDuringEvent = copyFromCc.ContactPersonDuringEvent;
        data.NewCountingCircle.ContactPersonAfterEvent = copyFromCc.ContactPersonAfterEvent;
        data.NewCountingCircle.ContactPersonSameDuringEventAsAfter = copyFromCc.ContactPersonSameDuringEventAsAfter;
        data.NewCountingCircle.Canton = copyFromCc.Canton;
    }

    private async Task EnsureNotInScheduledMerge(Guid id)
    {
        var hasScheduledMerger = await _repo.Query().AnyAsync(c => c.Id == id
                                                                   && c.MergeTarget != null
                                                                   && !c.MergeTarget.Merged);
        if (hasScheduledMerger)
        {
            throw new CountingCircleInScheduledMergeException();
        }
    }

    private async Task<bool> TryActivateMergeIfOverdue(CountingCircleAggregate countingCircle)
    {
        if (!countingCircle.MergeActivationOverdue)
        {
            return false;
        }

        if (!countingCircle.TryActivateMerge())
        {
            return false;
        }

        var aggregatesToSave = new List<CountingCircleAggregate> { countingCircle };
        foreach (var mergedCcId in countingCircle.MergerOrigin!.MergedCountingCircleIds)
        {
            var mergedCc = await _aggregateRepository.GetById<CountingCircleAggregate>(mergedCcId);
            mergedCc.SetMerged();
            aggregatesToSave.Add(mergedCc);
        }

        foreach (var aggregate in aggregatesToSave)
        {
            await _aggregateRepository.Save(aggregate);
        }

        return true;
    }

    private async Task<bool> TryActivateEVotingIfOverdue(CountingCircleAggregate countingCircle)
    {
        if (!countingCircle.EVotingActivationOverdue)
        {
            return false;
        }

        if (!countingCircle.TryActivateEVoting())
        {
            return false;
        }

        await _aggregateRepository.Save(countingCircle);
        return true;
    }

    private async Task EnsureCanCreate(Domain.CountingCircle countingCircle)
    {
        if (await _permissionService.IsOwnerOfCanton(countingCircle.Canton))
        {
            return;
        }

        throw new ForbiddenException();
    }

    private async Task EnsureCanEdit(CountingCircleAggregate countingCircle)
    {
        if (_auth.HasPermission(Permissions.CountingCircle.UpdateSameCanton) && await _permissionService.IsOwnerOfCanton(countingCircle.Canton))
        {
            return;
        }

        if (_auth.Tenant.Id == countingCircle.ResponsibleAuthority.SecureConnectId)
        {
            return;
        }

        throw new ForbiddenException();
    }

    private async Task EnsureCanDelete(CountingCircleAggregate countingCircle)
    {
        if (_auth.HasPermission(Permissions.CountingCircle.DeleteSameCanton) && await _permissionService.IsOwnerOfCanton(countingCircle.Canton))
        {
            return;
        }

        throw new ForbiddenException();
    }

    private async Task EnsureCanMerge(DomainOfInfluenceCanton canton)
    {
        if (_auth.HasPermission(Permissions.CountingCircle.MergeSameCanton) && await _permissionService.IsOwnerOfCanton(canton))
        {
            return;
        }

        throw new ForbiddenException();
    }
}
