// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Basis.Data.Repositories.Snapshot;

namespace Voting.Basis.Data.Repositories;

public class CountingCircleRepo : HasSnapshotDbRepository<CountingCircle, CountingCircleSnapshot>
{
    public CountingCircleRepo(DataContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    public override Task Delete(CountingCircle value, DateTime timestamp)
        => Delete(value, timestamp, true);

    public Task HardDelete(CountingCircle value, DateTime timestamp)
        => Delete(value, timestamp, false);

    protected override IQueryable<CountingCircleSnapshot> SnapshotIncludeQuery(IQueryable<CountingCircleSnapshot> set)
    {
        return set
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ResponsibleAuthority);
    }

    private async Task Delete(CountingCircle value, DateTime timestamp, bool softDelete)
    {
        if (IsTracked(value.Id, out var entity))
        {
            Context.Entry(entity).State = EntityState.Detached;
        }

        value.ModifiedOn = timestamp;

        if (value.State == CountingCircleState.Active)
        {
            throw new ValidationException("A deleted counting circle cannot have the active state");
        }

        if (softDelete)
        {
            Set.Update(value);
        }
        else
        {
            Set.Remove(value);
        }

        await CreateSnapshot(value, null, true);
        await Context.SaveChangesAsync();
    }
}
