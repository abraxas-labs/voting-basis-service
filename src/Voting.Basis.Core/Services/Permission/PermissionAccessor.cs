// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Permission;

public class PermissionAccessor
{
    /// <summary>
    /// Types of events, that change the permission structure.
    /// </summary>
    private static readonly IReadOnlySet<string> EventTypesWithPermissionChanges = new HashSet<string>
    {
        CantonSettingsCreated.Descriptor.FullName,
        CantonSettingsUpdated.Descriptor.FullName,
        CountingCircleCreated.Descriptor.FullName,
        CountingCircleUpdated.Descriptor.FullName,
        CountingCircleDeleted.Descriptor.FullName,
        CountingCircleMerged.Descriptor.FullName,
        CountingCirclesMergerActivated.Descriptor.FullName,
        DomainOfInfluenceCreated.Descriptor.FullName,
        DomainOfInfluenceUpdated.Descriptor.FullName,
        DomainOfInfluenceDeleted.Descriptor.FullName,
        DomainOfInfluenceCountingCircleEntriesUpdated.Descriptor.FullName,
    };

    private readonly AsyncLock _lock = new();
    private readonly IAuth _auth;
    private readonly PermissionService _permissionService;
    private readonly DomainOfInfluencePermissionRepo _permissionRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _domainOfInfluenceRepo;
    private readonly CantonSettingsRepo _cantonSettingsRepo;

    private bool _permissionsLoaded;
    private IReadOnlySet<DomainOfInfluenceCanton> _accessibleCantons = new HashSet<DomainOfInfluenceCanton>();
    private IReadOnlySet<Guid> _accessibleDomainOfInfluenceIds = new HashSet<Guid>();
    private IReadOnlySet<Guid> _accessibleCountingCircleIds = new HashSet<Guid>();

    public PermissionAccessor(
        IAuth auth,
        PermissionService permissionService,
        DomainOfInfluencePermissionRepo permissionRepo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DomainOfInfluence> domainOfInfluenceRepo,
        CantonSettingsRepo cantonSettingsRepo)
    {
        _auth = auth;
        _permissionService = permissionService;
        _permissionRepo = permissionRepo;
        _countingCircleRepo = countingCircleRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _cantonSettingsRepo = cantonSettingsRepo;
    }

    public async Task<bool> CanRead(EventProcessedMessage msg)
    {
        var forceLoadPermissions = EventTypesWithPermissionChanges.Contains(msg.EventType);
        if (!_permissionsLoaded || forceLoadPermissions)
        {
            await Reload(forceLoadPermissions);
        }

        if (msg.DomainOfInfluenceId.HasValue && !CanAccessDomainOfInfluence(msg.DomainOfInfluenceId.Value))
        {
            return false;
        }

        if (msg.CountingCircleId.HasValue && !CanAccessCountingCircle(msg.CountingCircleId.Value, msg.TenantId))
        {
            return false;
        }

        // only doi/cc related events are supported
        return msg.DomainOfInfluenceId.HasValue || msg.CountingCircleId.HasValue;
    }

    private bool CanAccessCountingCircle(Guid id, string tenantId)
    {
        if (_auth.HasPermission(Permissions.CountingCircle.ReadAll))
        {
            return true;
        }

        if (_auth.HasPermission(Permissions.CountingCircle.ReadSameCanton))
        {
            return _accessibleCountingCircleIds.Contains(id);
        }

        return _auth.HasPermission(Permissions.CountingCircle.Read)
               && (tenantId == _auth.Tenant.Id || _accessibleCountingCircleIds.Contains(id));
    }

    private bool CanAccessDomainOfInfluence(Guid id)
    {
        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadAll))
        {
            return true;
        }

        return _accessibleDomainOfInfluenceIds.Contains(id)
               && (_auth.HasPermission(Permissions.DomainOfInfluence.ReadSameCanton)
                   || _auth.HasPermission(Permissions.DomainOfInfluence.ReadSameTenant));
    }

    private async Task Reload(bool force)
    {
        if (!force && _permissionsLoaded)
        {
            return;
        }

        using var locker = await _lock.AcquireAsync();
        if (!force && _permissionsLoaded)
        {
            return;
        }

        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadAll)
            && _auth.HasPermission(Permissions.CountingCircle.ReadAll))
        {
            return;
        }

        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadSameCanton)
            || _auth.HasPermission(Permissions.CountingCircle.ReadSameCanton))
        {
            _accessibleCantons = await _cantonSettingsRepo.Query()
                .Where(x => x.SecureConnectId == _auth.Tenant.Id)
                .Select(x => x.Canton)
                .ToHashSetAsync();
        }

        if (_auth.HasPermission(Permissions.CountingCircle.ReadSameCanton))
        {
            _accessibleCountingCircleIds = await _countingCircleRepo.Query()
                .Where(x => _accessibleCantons.Contains(x.Canton))
                .Select(x => x.Id)
                .ToHashSetAsync();
        }
        else
        {
            var ccIds = await _permissionRepo.Query()
                .Where(x => x.TenantId == _auth.Tenant.Id)
                .Select(x => x.CountingCircleIds)
                .ToListAsync();
            _accessibleCountingCircleIds = ccIds.SelectMany(x => x).ToHashSet();
        }

        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadSameCanton))
        {
            _accessibleDomainOfInfluenceIds = await _domainOfInfluenceRepo.Query()
                .Where(x => _accessibleCantons.Contains(x.Canton))
                .Select(x => x.Id)
                .ToHashSetAsync();
        }
        else
        {
            var groups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
            _accessibleDomainOfInfluenceIds = groups.AccessibleDoiIds;
        }

        _permissionsLoaded = true;
    }
}
