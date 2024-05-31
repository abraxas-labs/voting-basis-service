// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class DomainOfInfluencePermissionRepo : DbRepository<DataContext, DomainOfInfluencePermissionEntry>
{
    public DomainOfInfluencePermissionRepo(DataContext context)
        : base(context)
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task Replace(IReadOnlyCollection<DomainOfInfluencePermissionEntry> entries)
    {
        await Context.Database.ExecuteSqlRawAsync($"TRUNCATE {DelimitedSchemaAndTableName}");
        if (entries.Count > 0)
        {
            await CreateRange(entries);
        }
    }
}
