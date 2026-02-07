// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Extensions;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Core.Services.Read;

public class ContestReader
{
    private static readonly TimeSpan MaxPreconfiguredDatesDelta = TimeSpan.FromDays(2 * 365);

    private readonly IDbRepository<DataContext, Contest> _repo;
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, ProportionalElection> _proportionalElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryMajorityElectionRepo;
    private readonly IDbRepository<DataContext, DateTime, PreconfiguredContestDate> _preconfiguredDatesRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly CantonSettingsRepo _cantonSettingsRepo;
    private readonly PublisherConfig _config;
    private readonly IAuth _auth;
    private readonly PermissionService _permissionService;
    private readonly IClock _clock;

    public ContestReader(
        IDbRepository<DataContext, Contest> repo,
        IDbRepository<DataContext, Vote> voteRepo,
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryMajorityElectionRepo,
        IDbRepository<DataContext, DateTime, PreconfiguredContestDate> preconfiguredDatesRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        CantonSettingsRepo cantonSettingsRepo,
        PublisherConfig config,
        IAuth auth,
        PermissionService permissionService,
        IClock clock)
    {
        _repo = repo;
        _voteRepo = voteRepo;
        _proportionalElectionRepo = proportionalElectionRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _secondaryMajorityElectionRepo = secondaryMajorityElectionRepo;
        _preconfiguredDatesRepo = preconfiguredDatesRepo;
        _hierarchyRepo = hierarchyRepo;
        _cantonSettingsRepo = cantonSettingsRepo;
        _config = config;
        _auth = auth;
        _permissionService = permissionService;
        _clock = clock;
    }

    public async Task<Contest> Get(Guid id)
    {
        var query = _repo.Query()
            .IgnoreQueryFilters() // Contests with a deleted DOI should still show
            .AsSplitQuery();

        if (_auth.HasAnyPermission(Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll))
        {
            query = query.Include(x => x.SimplePoliticalBusinesses
                    .OrderBy(pb => pb.DomainOfInfluence!.Type)
                    .ThenBy(pb => pb.PoliticalBusinessNumber)
                    .ThenBy(pb => pb.BusinessType)
                    .ThenBy(pb => pb.Id))
                .ThenInclude(x => x.DomainOfInfluence)
                .Include(c => c.ProportionalElectionUnions)
                .Include(c => c.MajorityElectionUnions);
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
                .ThenInclude(v => v.DomainOfInfluence)
                .Include(c => c.ProportionalElectionUnions.Where(u => u.SecureConnectId == _auth.Tenant.Id))
                .Include(c => c.MajorityElectionUnions.Where(u => u.SecureConnectId == _auth.Tenant.Id));
        }

        var contest = await query
            .Include(c => c.DomainOfInfluence)
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
        IReadOnlySet<Guid>? accessibleDois = null;
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
            .IgnoreQueryFilters() // Contests with a deleted DOI should still show
            .Include(c => c.DomainOfInfluence)
            .Order(states)
            .Select(c => new ContestSummary
            {
                Contest = c,
                ContestEntriesDetails = c.SimplePoliticalBusinesses
                    .Where(pb =>
                        pb.BusinessType != PoliticalBusinessType.SecondaryMajorityElection &&
                        (canReadAllPbs || accessibleDois!.Contains(pb.DomainOfInfluenceId)))
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
            .IgnoreQueryFilters() // Contests with a deleted DOI should still show
            .Where(c => c.Date < date.Date && c.DomainOfInfluence.Id == doiId &&
                        c.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id)
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

    public async Task<PoliticalBusinessSummary> GetPoliticalBusinessSummary(
        PoliticalBusinessType type,
        Guid politicalBusinessId)
    {
        var result = type switch
        {
            PoliticalBusinessType.Vote =>
                await _voteRepo.Query()
                    .IgnoreQueryFilters() // Votes with a deleted DOI should still show
                    .Include(x => x.DomainOfInfluence)
                    .Include(x => x.Ballots) // required for political business sub type
                    .Where(x => x.Id == politicalBusinessId)
                    .Select(x => new PoliticalBusinessSummary { PoliticalBusiness = x })
                    .FirstOrDefaultAsync()
                ?? throw new EntityNotFoundException(nameof(Vote), politicalBusinessId),
            PoliticalBusinessType.ProportionalElection =>
                await _proportionalElectionRepo.Query()
                    .IgnoreQueryFilters() // Proportional elections with a deleted DOI should still show
                    .Include(x => x.DomainOfInfluence)
                    .Where(x => x.Id == politicalBusinessId)
                    .Select(x => new PoliticalBusinessSummary
                    {
                        PoliticalBusiness = x,
                        PoliticalBusinessUnionDescription =
                            x.ProportionalElectionUnionEntries
                                .First().ProportionalElectionUnion
                                .Description ?? string.Empty,
                        PoliticalBusinessUnionId =
                            x.ProportionalElectionUnionEntries
                                .First()
                                .ProportionalElectionUnionId,
                    })
                    .FirstOrDefaultAsync() ??
                throw new EntityNotFoundException(nameof(ProportionalElection), politicalBusinessId),
            PoliticalBusinessType.MajorityElection =>
                await _majorityElectionRepo.Query()
                    .IgnoreQueryFilters() // Majority elections with a deleted DOI should still show
                    .Include(x => x.DomainOfInfluence)
                    .Where(x => x.Id == politicalBusinessId)
                    .Select(x => new PoliticalBusinessSummary
                    {
                        PoliticalBusiness = x,
                        PoliticalBusinessUnionDescription = x.MajorityElectionUnionEntries.First().MajorityElectionUnion.Description ?? string.Empty,
                        PoliticalBusinessUnionId = x.MajorityElectionUnionEntries.First().MajorityElectionUnionId,
                        ElectionGroupNumber = x.ElectionGroup!.Number.ToString() ?? string.Empty,
                        ElectionGroupId = x.ElectionGroup!.Id,
                    })
                    .FirstOrDefaultAsync() ??
                throw new EntityNotFoundException(nameof(MajorityElection), politicalBusinessId),
            PoliticalBusinessType.SecondaryMajorityElection =>
                await _secondaryMajorityElectionRepo.Query()
                    .IgnoreQueryFilters() // Elections with a deleted DOI should still show
                    .Include(x => x.PrimaryMajorityElection.DomainOfInfluence)
                    .Where(x => x.Id == politicalBusinessId)
                    .Select(x => new PoliticalBusinessSummary
                    {
                        PoliticalBusiness = x,
                        ElectionGroupNumber = x.ElectionGroup.Number.ToString() ?? string.Empty,
                        ElectionGroupId = x.ElectionGroup.Id,
                    })
                    .FirstOrDefaultAsync() ??
                throw new EntityNotFoundException(nameof(SecondaryMajorityElection), politicalBusinessId),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown type"),
        };

        if (_auth.HasPermission(Permissions.Contest.ReadAll))
        {
            return result;
        }

        if (_auth.HasPermission(Permissions.Contest.ReadSameCanton))
        {
            if (!await _permissionService.IsOwnerOfCanton(result.PoliticalBusiness.DomainOfInfluence!.Canton))
            {
                throw new EntityNotFoundException(nameof(PoliticalBusiness), politicalBusinessId);
            }

            return result;
        }

        var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
        if (!doiHierarchyGroups.AccessibleDoiIds.Contains(result.PoliticalBusiness.DomainOfInfluenceId))
        {
            throw new EntityNotFoundException(nameof(PoliticalBusiness), politicalBusinessId);
        }

        return result;
    }

    public async Task<IEnumerable<PoliticalBusinessSummary>> ListPoliticalBusinessSummaries(Guid contestId)
    {
        var query = _repo.Query()
            .IgnoreQueryFilters() // Political businesses with a deleted DOI should still show
            .AsSplitQuery();

        if (_auth.HasAnyPermission(Permissions.Contest.ReadSameCanton, Permissions.Contest.ReadAll))
        {
            query = query
                .Include(x => x.Votes)
                .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.Votes)
                .ThenInclude(x => x.Ballots) // required for political business sub type
                .Include(x => x.MajorityElections)
                .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.MajorityElections)
                .ThenInclude(x => x.MajorityElectionUnionEntries)
                .ThenInclude(x => x.MajorityElectionUnion)
                .Include(x => x.MajorityElections)
                .ThenInclude(x => x.ElectionGroup)
                .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.ElectionGroup)
                .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElectionUnion);
        }
        else
        {
            var doiHierarchyGroups = await _permissionService.GetAccessibleDomainOfInfluenceHierarchyGroups();
            query = query
                .Where(x => doiHierarchyGroups.TenantAndParentDoiIds.Contains(x.DomainOfInfluenceId))
                .Include(x => x.Votes
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId)))
                .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.MajorityElections
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId)))
                .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.MajorityElections
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId)))
                .ThenInclude(x => x.MajorityElectionUnionEntries)
                .ThenInclude(x => x.MajorityElectionUnion)
                .Include(x => x.MajorityElections
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId)))
                .ThenInclude(x => x.ElectionGroup)
                .Include(x => x.MajorityElections
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId)))
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.ElectionGroup)
                .Include(x => x.ProportionalElections
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId)))
                .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.ProportionalElections
                    .Where(pb => doiHierarchyGroups.AccessibleDoiIds.Contains(pb.DomainOfInfluenceId)))
                .ThenInclude(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElectionUnion);
        }

        var contest = await query
            .Include(c => c.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == contestId)
            ?? throw new EntityNotFoundException(nameof(Contest), contestId);

        if (_auth.HasPermission(Permissions.Contest.ReadSameCanton)
            && !_auth.HasPermission(Permissions.Contest.ReadAll)
            && !await _permissionService.IsOwnerOfCanton(contest.DomainOfInfluence.Canton))
        {
            throw new EntityNotFoundException(nameof(Contest), contestId);
        }

        var majorityElectionSummaries = contest.MajorityElections.Select(x => new PoliticalBusinessSummary
        {
            PoliticalBusiness = x,
            PoliticalBusinessUnionDescription =
                x.MajorityElectionUnionEntries.FirstOrDefault()?.MajorityElectionUnion.Description ?? string.Empty,
            PoliticalBusinessUnionId = x.MajorityElectionUnionEntries.FirstOrDefault()?.MajorityElectionUnionId,
            ElectionGroupNumber = x.ElectionGroup?.Number.ToString() ?? string.Empty,
            ElectionGroupId = x.ElectionGroup?.Id,
        }).ToList();

        var secondaryMajorityElectionSummaries = contest.MajorityElections.SelectMany(x => x.SecondaryMajorityElections)
            .Select(x => new PoliticalBusinessSummary
            {
                PoliticalBusiness = x,
                ElectionGroupNumber = x.ElectionGroup.Number.ToString(),
                ElectionGroupId = x.ElectionGroupId,
            }).ToList();

        var proportionalElectionSummaries = contest.ProportionalElections.Select(x => new PoliticalBusinessSummary
        {
            PoliticalBusiness = x,
            PoliticalBusinessUnionDescription =
                x.ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElectionUnion.Description ??
                string.Empty,
            PoliticalBusinessUnionId = x.ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElectionUnionId,
        }).ToList();

        var voteSummaries = contest.Votes.Select(x => new PoliticalBusinessSummary { PoliticalBusiness = x }).ToList();
        var politicalBusinesses = majorityElectionSummaries.Concat(secondaryMajorityElectionSummaries)
            .Concat(proportionalElectionSummaries).Concat(voteSummaries);
        return politicalBusinesses
            .OrderBy(x => x.PoliticalBusiness.DomainOfInfluence!.Type)
            .ThenBy(x => x.PoliticalBusiness.PoliticalBusinessNumber)
            .ThenBy(x => x.PoliticalBusiness.PoliticalBusinessType)
            .ThenBy(x => x.PoliticalBusiness.Id);
    }

    internal async Task<(SharedProto.ContestDateAvailability Availability, IEnumerable<Contest> Contests)>
        CheckAvailabilityInternal(
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
            return (SharedProto.ContestDateAvailability.SameAsPreConfiguredDate, []);
        }

        var maxDate = date.Add(_config.Contest.ContestCreationWarnPeriod);
        var minDate = date.Subtract(_config.Contest.ContestCreationWarnPeriod);
        if (await _repo.Query().AnyAsync(c =>
                c.Date >= minDate && c.Date <= maxDate && hierarchy.ParentIds.Contains(c.DomainOfInfluenceId)))
        {
            return (SharedProto.ContestDateAvailability.CloseToOtherContestDate, []);
        }

        return (SharedProto.ContestDateAvailability.Available, []);
    }

    private async Task<List<DomainOfInfluenceCanton>> GetAccessibleCantons()
    {
        return await _cantonSettingsRepo.Query()
            .Where(x => x.SecureConnectId == _auth.Tenant.Id)
            .Select(x => x.Canton)
            .ToListAsync();
    }
}
