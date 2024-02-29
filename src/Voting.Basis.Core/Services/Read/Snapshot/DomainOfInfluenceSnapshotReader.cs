// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Basis.Data.Repositories.Snapshot;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read.Snapshot;

public class DomainOfInfluenceSnapshotReader
{
    private readonly IDbRepository<DataContext, DomainOfInfluenceSnapshot> _repo;
    private readonly DomainOfInfluenceCountingCircleSnapshotRepo _doiCountingCirclesRepo;
    private readonly IAuth _auth;
    private readonly DomainOfInfluencePermissionBuilder _doiPermissionBuilder;
    private readonly DataContext _db;

    public DomainOfInfluenceSnapshotReader(
        IDbRepository<DataContext, DomainOfInfluenceSnapshot> repo,
        DomainOfInfluenceCountingCircleSnapshotRepo doiCountingCirclesRepo,
        IAuth auth,
        DomainOfInfluencePermissionBuilder doiPermissionBuilder,
        DataContext db)
    {
        _repo = repo;
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
        _auth = auth;
        _doiPermissionBuilder = doiPermissionBuilder;
        _db = db;
    }

    public async Task<List<DomainOfInfluenceSnapshot>> ListTree(DateTime dateTime, bool includeDeleted)
    {
        var dois = await _repo
            .Query()
            .ValidOn(dateTime)
            .Where(x => includeDeleted || !x.Deleted)
            .ToListAsync();
        var ccsByDoiIds = await _doiCountingCirclesRepo.CountingCirclesByDomainOfInfluenceId(dateTime, includeDeleted);

        if (!_auth.HasPermission(Permissions.DomainOfInfluenceHierarchy.ReadAll))
        {
            var authEntries = _doiPermissionBuilder.GetPermissionTreeSnapshot(dois, ccsByDoiIds)
                .Where(x => x.TenantId == _auth.Tenant.Id);
            var doiIds = authEntries.Select(x => x.DomainOfInfluenceId).ToList();
            var countingCircleIds = authEntries.SelectMany(x => x.CountingCircleIds).ToList();

            dois = dois
                .Where(doi => doiIds.Contains(doi.BasisId))
                .ToList();

            foreach (var key in ccsByDoiIds.Keys.ToList())
            {
                ccsByDoiIds[key] = ccsByDoiIds[key]
                    .Where(doiCc =>
                        countingCircleIds.Contains(doiCc.BasisCountingCircleId)
                        && doiIds.Contains(doiCc.BasisDomainOfInfluenceId))
                    .ToList();
            }
        }

        return DomainOfInfluenceTreeBuilder.BuildTree(dois, ccsByDoiIds);
    }

    public async Task<List<DomainOfInfluenceSnapshot>> ListForCountingCircle(string countingCircleKey, DateTime dateTime)
    {
        var countingCircleId = GuidParser.Parse(countingCircleKey);

        if (_auth.HasPermission(Permissions.DomainOfInfluence.ReadAll))
        {
            return await _repo.Query()
                .ValidOn(dateTime)
                .Where(doi => !doi.Deleted)
                .Join(
                    _db.DomainOfInfluenceCountingCircleSnapshots
                        .ValidOn(dateTime)
                        .Where(doiCc => doiCc.BasisCountingCircleId == countingCircleId),
                    doi => doi.BasisId,
                    doiCc => doiCc.BasisDomainOfInfluenceId,
                    (doi, _) => doi)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        var tenantId = _auth.Tenant.Id;
        var authEntries = await _doiPermissionBuilder.GetPermissionTreeSnapshot(dateTime, false, true);
        var doiIds = authEntries
            .Where(p => p.TenantId == tenantId && p.CountingCircleIds.Contains(countingCircleId))
            .Select(p => p.DomainOfInfluenceId)
            .ToList();

        return await _repo.Query()
            .ValidOn(dateTime)
            .Where(doi => !doi.Deleted && doiIds.Contains(doi.BasisId))
            .OrderBy(doi => doi.Name)
            .ToListAsync();
    }
}
