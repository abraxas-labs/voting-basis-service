// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories;

public class DomainOfInfluenceHierarchyRepo : DbRepository<DataContext, DomainOfInfluenceHierarchy>
{
    public DomainOfInfluenceHierarchyRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<List<Guid>> GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(Guid doiId)
    {
        var result = new List<Guid>();
        var hierarchy = await Query().FirstOrDefaultAsync(h => h.DomainOfInfluenceId == doiId);

        result.Add(doiId);

        if (hierarchy != null)
        {
            result.AddRange(hierarchy.ParentIds);
        }

        return result;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task Replace(IEnumerable<DomainOfInfluenceHierarchy> entries)
    {
        await Context.Database.ExecuteSqlRawAsync($"TRUNCATE {DelimitedSchemaAndTableName}");
        Set.AddRange(entries);
        await Context.SaveChangesAsync();
    }
}
