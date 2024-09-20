// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class CantonSettingsRepo : DbRepository<DataContext, CantonSettings>
{
    public CantonSettingsRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<CantonSettings?> GetByDomainOfInfluenceCanton(DomainOfInfluenceCanton canton)
    {
        return await Query().SingleOrDefaultAsync(x => x.Canton == canton);
    }
}
