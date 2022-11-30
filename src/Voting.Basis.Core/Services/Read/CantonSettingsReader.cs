// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class CantonSettingsReader
{
    private readonly IDbRepository<DataContext, CantonSettings> _repo;
    private readonly IAuth _auth;

    public CantonSettingsReader(
        IDbRepository<DataContext, CantonSettings> repo,
        IAuth auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public async Task<CantonSettings> Get(Guid id)
    {
        _auth.EnsureAdminOrElectionAdmin();
        return await Query().FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    public async Task<IEnumerable<CantonSettings>> List()
    {
        _auth.EnsureAdminOrElectionAdmin();
        return await Query().ToListAsync();
    }

    private IQueryable<CantonSettings> Query()
    {
        return _repo.Query()
            .Where(c => _auth.IsAdmin() || c.SecureConnectId == _auth.Tenant.Id)
            .Include(x => x.EnabledVotingCardChannels.OrderBy(y => y.VotingChannel).ThenBy(y => y.Valid))
            .OrderBy(c => c.Canton);
    }
}
