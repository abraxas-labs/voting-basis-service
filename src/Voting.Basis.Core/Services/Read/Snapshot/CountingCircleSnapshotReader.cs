// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read.Snapshot;

public class CountingCircleSnapshotReader
{
    private readonly IDbRepository<DataContext, CountingCircleSnapshot> _repo;
    private readonly IAuth _auth;
    private readonly DomainOfInfluencePermissionBuilder _doiPermissionBuilder;
    private readonly DataContext _db;

    public CountingCircleSnapshotReader(
        IDbRepository<DataContext, CountingCircleSnapshot> repo,
        IAuth auth,
        DomainOfInfluencePermissionBuilder doiPermissionBuilder,
        DataContext db)
    {
        _repo = repo;
        _auth = auth;
        _doiPermissionBuilder = doiPermissionBuilder;
        _db = db;
    }

    public async Task<List<CountingCircleSnapshot>> List(DateTime dateTime, bool includeDeleted)
    {
        var query = _repo.Query();
        if (!_auth.HasPermission(Permissions.CountingCircle.ReadAll))
        {
            var authEntries = await _doiPermissionBuilder.GetPermissionTreeSnapshot(dateTime, false, includeDeleted);
            var doiPermissionCcIds = authEntries
                .Where(p => p.TenantId == _auth.Tenant.Id)
                .SelectMany(p => p.CountingCircleIds)
                .ToList();

            query = query.Where(cc =>
                doiPermissionCcIds.Contains(cc.BasisId)
                || cc.ResponsibleAuthority.SecureConnectId == _auth.Tenant.Id);
        }

        return await query
            .ValidOn(dateTime)
            .Where(cc => includeDeleted || !cc.Deleted)
            .Include(cc => cc.ResponsibleAuthority)
            .Include(cc => cc.ContactPersonDuringEvent)
            .Include(cc => cc.ContactPersonAfterEvent)
            .OrderBy(cc => cc.State)
            .ThenBy(cc => cc.Name)
            .ToListAsync();
    }

    public async Task<List<DomainOfInfluenceCountingCircleSnapshot>> ListForDomainOfInfluence(string domainOfInfluenceKey, DateTime dateTime)
    {
        var domainOfInfluenceId = GuidParser.Parse(domainOfInfluenceKey);

        IQueryable<CountingCircleSnapshot> query = _repo.Query()
            .ValidOn(dateTime)
            .Where(cc => !cc.Deleted)
            .Include(cc => cc.ResponsibleAuthority)
            .Include(cc => cc.ContactPersonDuringEvent)
            .Include(cc => cc.ContactPersonAfterEvent);

        if (!_auth.HasPermission(Permissions.CountingCircle.ReadAll))
        {
            var authEntries = await _doiPermissionBuilder.GetPermissionTreeSnapshot(dateTime, true, false);
            var doiPermission = authEntries
                .FirstOrDefault(p => p.DomainOfInfluenceId == domainOfInfluenceId && p.TenantId == _auth.Tenant.Id)
                ?? throw new EntityNotFoundException(domainOfInfluenceId);

            query = query.Where(cc => doiPermission.CountingCircleIds.Contains(cc.BasisId));
        }

        return await query
            .Join(
                _db.DomainOfInfluenceCountingCircleSnapshots
                    .ValidOn(dateTime)
                    .Where(doiCc => doiCc.BasisDomainOfInfluenceId == domainOfInfluenceId),
                cc => cc.BasisId,
                doiCc => doiCc.BasisCountingCircleId,
                (cc, doiCc) => new DomainOfInfluenceCountingCircleSnapshot
                {
                    Id = doiCc.BasisId,
                    BasisId = doiCc.BasisId,
                    Inherited = doiCc.Inherited,
                    BasisCountingCircleId = doiCc.BasisCountingCircleId,
                    BasisDomainOfInfluenceId = doiCc.BasisDomainOfInfluenceId,
                    CountingCircle = cc,
                })
            .OrderBy(doiCc => doiCc.CountingCircle.Name)
            .ToListAsync();
    }
}
