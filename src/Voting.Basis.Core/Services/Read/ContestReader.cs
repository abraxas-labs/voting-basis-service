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
    private readonly CantonSettingsRepo _cantonSettingsRepo;
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
        CantonSettingsRepo cantonSettingsRepo,
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
        _cantonSettingsRepo = cantonSettingsRepo;
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

        if (_auth.HasAnyPermission(Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll))
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

        var contest = await query
            .Include(c => c.DomainOfInfluence)
            .Include(c => c.ProportionalElectionUnions)
            .Include(c => c.MajorityElectionUnions)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(nameof(Contest), id);

        if (_auth.HasPermission(Permissions.Contest.ReadSameCanton)
            && !_auth.HasPermission(Permissions.Contest.ReadAll)
            && !await _permissionService.IsOwnerOfCanton(contest.DomainOfInfluence.Canton))
        {
            throw new EntityNotFoundException(nameof(Contest), id);
        }

        return contest;
    }

    public async Task<IEnumerable<ContestSummary>> ListSummaries(IReadOnlyCollection<ContestState> states)
    {
        var query = _repo.Query();

        if (states.Count > 0)
        {
            query = query.Where(x => states.Contains(x.State));
        }

        var canReadAllPbs = false;
        List<Guid>? accessibleDois = null;
        if (_auth.HasPermission(Permissions.Contest.ReadAll))
        {
            // no restrictions
            canReadAllPbs = true;
        }
        else if (_auth.HasPermission(Permissions.Contest.ReadSameCanton))
        {
            canReadAllPbs = true;
            var cantons = await GetAccessibleCantons();
            query = query.Where(x => cantons.Contains(x.DomainOfInfluence.Canton));
        }
        else
        {
            var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
            accessibleDois = doiHierarchyGroups.AccessibleDoiIds;
            query = query.Where(x => doiHierarchyGroups.TenantAndParentDoiIds.Contains(x.DomainOfInfluenceId));
        }

        return await query
            .Include(c => c.DomainOfInfluence)
            .Order(states)
            .Select(c => new ContestSummary
            {
                Contest = c,
                ContestEntriesDetails = c.SimplePoliticalBusinesses
                    .Where(pb => pb.BusinessType != PoliticalBusinessType.SecondaryMajorityElection && (canReadAllPbs || accessibleDois!.Contains(pb.DomainOfInfluenceId)))
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

    public async Task ListenToContestOverviewChanges(
        Func<ContestOverviewChangeMessage, Task> listener,
        CancellationToken cancellationToken)
    {
        Func<Contest, bool> contestFilter;
        if (_auth.HasPermission(Permissions.Contest.ReadAll))
        {
            contestFilter = _ => true;
        }
        else if (_auth.HasPermission(Permissions.Contest.ReadSameCanton))
        {
            var cantons = await GetAccessibleCantons();
            contestFilter = contest => cantons.Contains(contest.DomainOfInfluence.Canton);
        }
        else
        {
            var tenantAndParentDoiIds = (await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups()).TenantAndParentDoiIds;
            contestFilter = contest => tenantAndParentDoiIds.Contains(contest.DomainOfInfluenceId);
        }

        await _contestOverviewChangeListener.Listen(
            e => e.Contest.Data != null && contestFilter(e.Contest.Data),
            listener,
            cancellationToken);
    }

    public async Task ListenToContestDetailsChanges(
        Guid contestId,
        Func<ContestDetailsChangeMessage, Task> listener,
        CancellationToken cancellationToken)
    {
        var doi = await _repo.Query()
            .Where(x => x.Id == contestId)
            .Select(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Contest), contestId);
        await _permissionService.EnsureCanReadDomainOfInfluence(doi);

        Func<ContestDetailsChangeMessage, bool> filter = _ => true;
        if (_auth.HasPermission(Permissions.Contest.ReadTenantHierarchy))
        {
            var accessibleDoiIds = (await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups()).AccessibleDoiIds;
            filter = e => e.PoliticalBusinessUnion?.Data != null ||
                (e.PoliticalBusiness?.Data != null && accessibleDoiIds.Contains(e.PoliticalBusiness.Data.DomainOfInfluenceId)) ||
                (e.ElectionGroup?.Data != null && accessibleDoiIds.Contains(e.ElectionGroup.Data.PrimaryMajorityElection.DomainOfInfluenceId));
        }

        await _contestDetailsChangeListener.Listen(
            e => e.ContestId.HasValue &&
                 e.ContestId == contestId &&
                 filter(e),
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

    private async Task<List<DomainOfInfluenceCanton>> GetAccessibleCantons()
    {
        return await _cantonSettingsRepo.Query()
            .Where(x => x.SecureConnectId == _auth.Tenant.Id)
            .Select(x => x.Canton)
            .ToListAsync();
    }
}
