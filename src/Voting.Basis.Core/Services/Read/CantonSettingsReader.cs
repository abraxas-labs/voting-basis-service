// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class CantonSettingsReader
{
    private readonly CantonSettingsRepo _repo;
    private readonly IAuth _auth;

    public CantonSettingsReader(
        CantonSettingsRepo repo,
        IAuth auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public async Task<CantonSettings> Get(Guid id)
    {
        return await Query().FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    public async Task<CantonSettings> Get(DomainOfInfluenceCanton canton)
    {
        return await _repo.GetByDomainOfInfluenceCanton(canton)
            ?? new CantonSettings { Canton = canton };
    }

    public async Task<IEnumerable<CantonSettings>> List()
    {
        return await Query().ToListAsync();
    }

    private IQueryable<CantonSettings> Query()
    {
        var canReadAll = _auth.HasPermission(Permissions.CantonSettings.ReadAll);
        return _repo.Query()
            .AsSplitQuery()
            .Where(c => canReadAll || c.SecureConnectId == _auth.Tenant.Id)
            .Include(x => x.EnabledVotingCardChannels.OrderBy(y => y.VotingChannel).ThenBy(y => y.Valid))
            .Include(x => x.CountingCircleResultStateDescriptions.OrderBy(y => y.State))
            .OrderBy(c => c.Canton);
    }
}
