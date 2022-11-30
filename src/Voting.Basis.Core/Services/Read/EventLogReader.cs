// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class EventLogReader
{
    private readonly IDbRepository<DataContext, EventLog> _repo;
    private readonly IAuth _auth;
    private readonly IValidator<Pageable> _pageableValidator;

    public EventLogReader(
        IDbRepository<DataContext, EventLog> repo,
        IAuth auth,
        IValidator<Pageable> pageableValidator)
    {
        _repo = repo;
        _auth = auth;
        _pageableValidator = pageableValidator;
    }

    public async Task<Page<EventLog>> List(Pageable pageable)
    {
        _auth.EnsureAdminOrElectionAdmin();
        _pageableValidator.ValidateAndThrow(pageable);

        var tenantId = _auth.Tenant.Id;
        return await _repo.Query()
            .Where(e => _auth.IsAdmin() || e.EventTenant!.TenantId == tenantId)
            .Include(e => e.EventTenant)
            .Include(e => e.EventUser)
            .OrderByDescending(e => e.Timestamp)
            .ToPageAsync(pageable);
    }
}
