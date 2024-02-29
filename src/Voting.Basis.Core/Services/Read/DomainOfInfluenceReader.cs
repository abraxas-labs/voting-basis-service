// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.ObjectStorage;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class DomainOfInfluenceReader
{
    private readonly IDbRepository<DataContext, DomainOfInfluence> _repo;
    private readonly DomainOfInfluenceCountingCircleRepo _doiCountingCirclesRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluencePermissionEntry> _permissionRepo;
    private readonly IDbRepository<DataContext, DomainOfInfluenceParty> _doiPartyRepo;
    private readonly IAuth _auth;
    private readonly PermissionService _permissionService;
    private readonly DomainOfInfluenceLogoStorage _logoStorage;

    public DomainOfInfluenceReader(
        IDbRepository<DataContext, DomainOfInfluence> repo,
        IDbRepository<DataContext, DomainOfInfluencePermissionEntry> permissionRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        DomainOfInfluenceCountingCircleRepo doiCountingCirclesRepo,
        IDbRepository<DataContext, DomainOfInfluenceParty> doiPartyRepo,
        IAuth auth,
        PermissionService permissionService,
        DomainOfInfluenceLogoStorage logoStorage)
    {
        _repo = repo;
        _permissionRepo = permissionRepo;
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
        _hierarchyRepo = hierarchyRepo;
        _doiPartyRepo = doiPartyRepo;
        _auth = auth;
        _permissionService = permissionService;
        _logoStorage = logoStorage;
    }

    public async Task<DomainOfInfluence> Get(Guid id)
    {
        var query = _repo.Query()
            .AsSplitQuery()
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVoterParticipationConfigurations!
                    .OrderBy(y => y.MainLevel)
                    .ThenBy(y => y.ComparisonLevel))
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonCountOfVotersConfigurations!
                    .OrderBy(y => y.Category))
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVotingChannelConfigurations!
                    .OrderBy(y => y.VotingChannel))
            .AsQueryable();

        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadAll))
        {
            var domainOfInfluence = await query
                                        .Include(d => d.Children.OrderBy(c => c.SortNumber).ThenBy(c => c.ShortName))
                                        .Include(x => x.ExportConfigurations)
                                        .Include(d => d.CountingCircles.OrderBy(c => c.CountingCircle.Name))
                                            .ThenInclude(c => c.CountingCircle)
                                                .ThenInclude(c => c.ResponsibleAuthority)
                                        .FirstOrDefaultAsync(d => d.Id == id)
                                    ?? throw new EntityNotFoundException(id);
            domainOfInfluence.SortExportConfigurations();
            domainOfInfluence.Parties = await ListParties(domainOfInfluence.Id);
            return domainOfInfluence;
        }

        var tenantId = _auth.Tenant.Id;
        var authEntry = await _permissionRepo.Query()
                            .FirstOrDefaultAsync(p => p.DomainOfInfluenceId == id && p.TenantId == tenantId)
                        ?? throw new EntityNotFoundException(id);

        if (!authEntry.IsParent)
        {
            query = query.Include(d => d.Children.OrderBy(c => c.SortNumber).ThenBy(c => c.ShortName));
        }

        var doi = await query.FirstOrDefaultAsync(d => d.Id == id)
                  ?? throw new EntityNotFoundException(id);
        doi.CountingCircles = await _doiCountingCirclesRepo.Query()
            .Where(c => c.DomainOfInfluenceId == id && authEntry.CountingCircleIds.Contains(c.CountingCircleId))
            .Include(c => c.CountingCircle)
            .ThenInclude(c => c.ResponsibleAuthority)
            .OrderBy(c => c.CountingCircle.Name)
            .ToListAsync();
        doi.SortExportConfigurations();
        doi.Parties = await ListParties(doi.Id);
        return doi;
    }

    public async Task<List<DomainOfInfluence>> ListForSecureConnectId(string secureConnectId)
    {
        if (!_auth.HasPermission(Permissions.DomainOfInfluence.ReadAll) && _auth.Tenant.Id != secureConnectId)
        {
            throw new ForbiddenException("Non-admins may only list the domain of influences for their own tenant");
        }

        return await _repo.Query()
            .Where(doi => doi.SecureConnectId == secureConnectId)
            .ToListAsync();
    }

    public async Task<List<DomainOfInfluence>> ListForPoliticalBusiness(Guid contestDomainOfInfluenceId)
    {
        var childDoiIds = await _hierarchyRepo.Query()
            .Where(h => h.DomainOfInfluenceId == contestDomainOfInfluenceId)
            .Select(h => h.ChildIds)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(contestDomainOfInfluenceId);

        var hierarchyDoiIds = childDoiIds.Append(contestDomainOfInfluenceId).ToList();

        return await _repo.Query()
            .Where(doi => doi.SecureConnectId == _auth.Tenant.Id && hierarchyDoiIds.Contains(doi.Id))
            .ToListAsync();
    }

    public async Task<List<DomainOfInfluence>> ListForCountingCircle(Guid countingCircleId)
    {
        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadAll))
        {
            return await _repo.Query()
                .Where(d => d.CountingCircles.Any(c => c.CountingCircleId == countingCircleId))
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        var tenantId = _auth.Tenant.Id;
        var doiIds = _permissionRepo.Query()
            .Where(p => p.TenantId == tenantId)
            .AsEnumerable() // ef core npgsql cant translate this, https://github.com/npgsql/efcore.pg/issues/460
            .Where(p => p.CountingCircleIds.Contains(countingCircleId))
            .Select(p => p.DomainOfInfluenceId)
            .ToList();
        return await _repo.Query()
            .Where(d => doiIds.Contains(d.Id))
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<DomainOfInfluence>> ListTree()
    {
        if (_auth.HasPermission(Permissions.DomainOfInfluenceHierarchy.ReadAll))
        {
            return await ListAll();
        }

        var authEntries = await _permissionRepo.Query()
            .Where(x => x.TenantId == _auth.Tenant.Id)
            .ToListAsync();
        var doiIds = authEntries.ConvertAll(x => x.DomainOfInfluenceId);
        var countingCircleIds = authEntries.SelectMany(x => x.CountingCircleIds).ToList();
        var domainsOfInfluence = await _repo.Query()
            .Where(d => doiIds.Contains(d.Id))
            .ToListAsync();
        var countingCirclesByDoiId =
            await _doiCountingCirclesRepo.CountingCirclesByDomainOfInfluenceId(countingCircleIds, doiIds);
        return DomainOfInfluenceTreeBuilder.BuildTree(domainsOfInfluence, countingCirclesByDoiId);
    }

    /// <summary>
    /// Creates a hierarchical list of all existing domain of influence including child and parent information.
    /// </summary>
    /// <returns>List of domain of influences.</returns>
    public async Task<List<DomainOfInfluence>> ListAll()
    {
        var allDomains = await _repo.Query().ToListAsync();
        var countingCircles = await _doiCountingCirclesRepo.CountingCirclesByDomainOfInfluenceId();
        return DomainOfInfluenceTreeBuilder.BuildTree(allDomains, countingCircles);
    }

    public async Task<DomainOfInfluenceCantonDefaults> GetCantonDefaults(Guid domainOfInfluenceId)
    {
        var doi = await _repo.GetByKey(domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        await _permissionService.EnsureCanReadDomainOfInfluence(domainOfInfluenceId);
        return doi.CantonDefaults;
    }

    public async Task<Uri> GetLogoUrl(Guid doiId)
    {
        var doi = await _repo.GetByKey(doiId)
            ?? throw new EntityNotFoundException(nameof(DomainOfInfluence), doiId);

        await _permissionService.EnsureCanReadDomainOfInfluence(doi.Id);

        if (doi.LogoRef == null)
        {
            throw new EntityNotFoundException("DomainOfInfluenceLogo", doiId);
        }

        return await _logoStorage.GetPublicDownloadUrl(doi.LogoRef);
    }

    public async Task<List<DomainOfInfluenceParty>> ListParties(Guid doiId)
    {
        await _permissionService.EnsureCanReadDomainOfInfluence(doiId);
        return await GetAllPartiesIncludingInheritedForDomainOfInfluence(doiId);
    }

    internal async Task EnsurePartyExistsAndIsAccessibleByDomainOfInfluence(Guid partyId, Guid doiId)
    {
        var hierarchicalGreaterOrSelfDoiIds = await _hierarchyRepo.GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(doiId);
        var partyExists = await _doiPartyRepo.Query()
            .AnyAsync(x => x.Id == partyId && hierarchicalGreaterOrSelfDoiIds.Contains(x.DomainOfInfluenceId));

        if (!partyExists)
        {
            throw new EntityNotFoundException(new { partyId, doiId });
        }
    }

    internal async Task<IReadOnlySet<Guid>> GetPartyIds(Guid doiId)
    {
        var query = await QueryAllPartiesIncludingInheritedForDomainOfInfluence(doiId);
        return await query
            .Select(x => x.Id)
            .AsAsyncEnumerable()
            .ToHashSetAsync();
    }

    private async Task<List<DomainOfInfluenceParty>> GetAllPartiesIncludingInheritedForDomainOfInfluence(Guid doiId)
    {
        var query = await QueryAllPartiesIncludingInheritedForDomainOfInfluence(doiId);
        return await query
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    private async Task<IQueryable<DomainOfInfluenceParty>> QueryAllPartiesIncludingInheritedForDomainOfInfluence(Guid doiId)
    {
        var hierarchicalGreaterOrSelfDoiIds = await _hierarchyRepo.GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(doiId);
        return _doiPartyRepo.Query().Where(x => hierarchicalGreaterOrSelfDoiIds.Contains(x.DomainOfInfluenceId));
    }
}
