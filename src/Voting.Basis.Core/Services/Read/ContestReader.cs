// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Extensions;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
using Voting.Lib.Messaging;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Core.Services.Read;

public class ContestReader
{
    private static readonly TimeSpan MaxPreconfiguredDatesDelta = TimeSpan.FromDays(2 * 365);

    private readonly IDbRepository<DataContext, Contest> _repo;
    private readonly IDbRepository<DataContext, DateTime, PreconfiguredContestDate> _preconfiguredDatesRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly PublisherConfig _config;
    private readonly IAuth _auth;
    private readonly PermissionService _permissionService;
    private readonly IClock _clock;
    private readonly MessageConsumerHub<ContestOverviewChangeMessage> _contestOverviewChangeListener;
    private readonly MessageConsumerHub<ContestDetailsChangeMessage> _contestDetailsChangeListener;

    public ContestReader(
        IDbRepository<DataContext, Contest> repo,
        IDbRepository<DataContext, DateTime, PreconfiguredContestDate> preconfiguredDatesRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        PublisherConfig config,
        IAuth auth,
        PermissionService permissionService,
        IClock clock,
        MessageConsumerHub<ContestOverviewChangeMessage> contestOverviewChangeListener,
        MessageConsumerHub<ContestDetailsChangeMessage> contestDetailsChangeListener)
    {
        _repo = repo;
        _preconfiguredDatesRepo = preconfiguredDatesRepo;
        _hierarchyRepo = hierarchyRepo;
        _config = config;
        _auth = auth;
        _permissionService = permissionService;
        _clock = clock;
        _contestOverviewChangeListener = contestOverviewChangeListener;
        _contestDetailsChangeListener = contestDetailsChangeListener;
    }

    public async Task<Contest> Get(Guid id)
    {
        var query = _repo.Query().AsSplitQuery();

        if (_auth.HasPermission(Permissions.Contest.ReadAll))
        {
            query = query.Include(x => x.SimplePoliticalBusinesses
                    .OrderBy(pb => pb.DomainOfInfluence!.Type)
                    .ThenBy(pb => pb.PoliticalBusinessNumber)
                    .ThenBy(pb => pb.BusinessType)
                    .ThenBy(pb => pb.Id))
                .ThenInclude(x => x.DomainOfInfluence);
        }
        else
        {
            var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
            query = query
                .Where(x => doiHierarchyGroups.TenantAndParentDoiIds.Contains(x.DomainOfInfluenceId))
                .Include(c => c.SimplePoliticalBusinesses
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId))
                    .OrderBy(pb => pb.DomainOfInfluence!.Type)
                    .ThenBy(pb => pb.PoliticalBusinessNumber)
                    .ThenBy(pb => pb.BusinessType)
                    .ThenBy(pb => pb.Id))
                .ThenInclude(v => v.DomainOfInfluence);
        }

        return await query
                   .Include(c => c.DomainOfInfluence)
                   .Include(c => c.ProportionalElectionUnions)
                   .Include(c => c.MajorityElectionUnions)
                   .FirstOrDefaultAsync(x => x.Id == id)
               ?? throw new EntityNotFoundException(nameof(Contest), id);
    }

    public async Task<IEnumerable<ContestSummary>> ListSummaries(IReadOnlyCollection<ContestState> states)
    {
        var query = _repo.Query();

        if (states.Count > 0)
        {
            query = query.Where(x => states.Contains(x.State));
        }

        var canReadAll = _auth.HasPermission(Permissions.Contest.ReadAll);
        List<Guid>? accessibleGuids = null;
        if (!canReadAll)
        {
            var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
            accessibleGuids = doiHierarchyGroups.AccessibleDoiIds;
            query = query.Where(x => doiHierarchyGroups.TenantAndParentDoiIds.Contains(x.DomainOfInfluenceId));
        }

        return await query
            .Include(c => c.DomainOfInfluence)
            .Order(states)
            .Select(c => new ContestSummary
            {
                Contest = c,
                ContestEntriesDetails = c.SimplePoliticalBusinesses
                    .Where(pb => pb.BusinessType != PoliticalBusinessType.SecondaryMajorityElection && (canReadAll || accessibleGuids!.Contains(pb.DomainOfInfluenceId)))
                    .GroupBy(x => x.DomainOfInfluence!.Type)
                    .Select(x => new ContestSummaryEntryDetails
                    {
                        DomainOfInfluenceType = x.Key,
                        ContestEntriesCount = x.Count(),
                    })
                    .OrderBy(x => x.DomainOfInfluenceType)
                    .ToList(),
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<Contest>> ListPast(DateTime date, Guid doiId)
    {
        return await _repo.Query()
            .Where(c => c.Date < date.Date && c.DomainOfInfluence.Id == doiId && c.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<PreconfiguredContestDate>> ListFuturePreconfiguredDates()
    {
        var from = _clock.UtcNow.Date;
        var to = from.Add(MaxPreconfiguredDatesDelta).Date.AddDays(1);

        return await _preconfiguredDatesRepo.Query()
            .Where(p => p.Id >= from && p.Id <= to)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<SharedProto.ContestDateAvailability> CheckAvailability(DateTime date, Guid domainOfInfluenceId)
    {
        if (!await _hierarchyRepo.Query()
                .AnyAsync(h => h.TenantId == _auth.Tenant.Id && h.DomainOfInfluenceId == domainOfInfluenceId))
        {
            throw new ValidationException("Invalid domain of influence, does not belong to this tenant");
        }

        return (await CheckAvailabilityInternal(date, domainOfInfluenceId)).Availability;
    }

    public async Task<List<ContestCountingCircleOption>> ListCountingCircleOptions(Guid contestId)
    {
        var contest = await _repo
                          .Query()
                          .Include(x => x.CountingCircleOptions).ThenInclude(x => x.CountingCircle)
                          .FirstOrDefaultAsync(c => c.Id == contestId)
                      ?? throw new EntityNotFoundException(contestId);

        await _permissionService.EnsureCanReadContest(contest);

        return contest.CountingCircleOptions
            .OrderBy(x => x.CountingCircle!.Name)
            .ToList();
    }

    public async Task ListenToContestOverviewChanges(
        Func<ContestOverviewChangeMessage, Task> listener,
        CancellationToken cancellationToken)
    {
        var canReadAll = _auth.HasPermission(Permissions.Contest.ReadAll);
        var tenantAndParentDoiIds = canReadAll ? new() : (await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups()).TenantAndParentDoiIds;

        await _contestOverviewChangeListener.Listen(
            e => e.Contest!.Data != null &&
                (
                    canReadAll ||
                    tenantAndParentDoiIds.Contains(e.Contest.Data.DomainOfInfluenceId)
                ),
            listener,
            cancellationToken);
    }

    public async Task ListenToContestDetailsChanges(
        Guid contestId,
        Func<ContestDetailsChangeMessage, Task> listener,
        CancellationToken cancellationToken)
    {
        var canReadAll = _auth.HasPermission(Permissions.Contest.ReadAll);
        var accessibleDoiIds = canReadAll ? new() : (await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups()).AccessibleDoiIds;

        await _contestDetailsChangeListener.Listen(
            e => e.ContestId.HasValue &&
                 e.ContestId == contestId &&
                 (
                     canReadAll ||
                     e.PoliticalBusinessUnion?.Data != null ||
                     (e.PoliticalBusiness?.Data != null && accessibleDoiIds.Contains(e.PoliticalBusiness.Data.DomainOfInfluenceId)) ||
                     (e.ElectionGroup?.Data != null && accessibleDoiIds.Contains(e.ElectionGroup.Data.PrimaryMajorityElection.DomainOfInfluenceId))
                 ),
            listener,
            cancellationToken);
    }

    internal async Task<(SharedProto.ContestDateAvailability Availability, IEnumerable<Contest> Contests)> CheckAvailabilityInternal(
        DateTime date,
        Guid doiId)
    {
        date = date.Date;
        var tenantId = _auth.Tenant.Id;
        var hierarchy = await _hierarchyRepo.Query()
            .FirstOrDefaultAsync(h => h.TenantId == tenantId && h.DomainOfInfluenceId == doiId);

        if (hierarchy == null)
        {
            throw new ValidationException("Invalid DomainOfInfluence id, does not belong to this tenant");
        }

        var contestsOnThisDate = await _repo.Query()
            .Where(c => c.Date == date)
            .ToListAsync();

        var existingContests = contestsOnThisDate
            .Where(c => c.DomainOfInfluenceId == doiId || hierarchy.ParentIds.Contains(c.DomainOfInfluenceId));
        if (existingContests.Any())
        {
            return (SharedProto.ContestDateAvailability.AlreadyExists, existingContests);
        }

        var childContests = contestsOnThisDate.Where(c => hierarchy.ChildIds.Contains(c.DomainOfInfluenceId));
        if (childContests.Any())
        {
            return (SharedProto.ContestDateAvailability.ExistsOnChildTenant, childContests);
        }

        if (await _preconfiguredDatesRepo.ExistsByKey(date))
        {
            return (SharedProto.ContestDateAvailability.SameAsPreConfiguredDate, Enumerable.Empty<Contest>());
        }

        var maxDate = date.Add(_config.Contest.ContestCreationWarnPeriod);
        var minDate = date.Subtract(_config.Contest.ContestCreationWarnPeriod);
        if (await _repo.Query().AnyAsync(c =>
                c.Date >= minDate && c.Date <= maxDate && hierarchy.ParentIds.Contains(c.DomainOfInfluenceId)))
        {
            return (SharedProto.ContestDateAvailability.CloseToOtherContestDate, Enumerable.Empty<Contest>());
        }

        return (SharedProto.ContestDateAvailability.Available, Enumerable.Empty<Contest>());
    }
}
