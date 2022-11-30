// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Lib.Database.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Data.Repositories.Snapshot;

public class HasSnapshotDbRepository<TEntity, TSnapshotEntity> : DbRepository<DataContext, TEntity>
    where TEntity : BaseEntity, IHasSnapshotEntity<TSnapshotEntity>, new()
    where TSnapshotEntity : class, ISnapshotEntity, new()
{
    public HasSnapshotDbRepository(DataContext context, IMapper mapper)
        : base(context)
    {
        Mapper = mapper;
        SnapshotSet = Context.Set<TSnapshotEntity>();
    }

    protected DbSet<TSnapshotEntity> SnapshotSet { get; }

    protected IMapper Mapper { get; }

    public virtual async Task Create(TEntity value, DateTime timestamp)
    {
        value.CreatedOn = timestamp;
        value.ModifiedOn = timestamp;

        Context.Add(value);
        await CreateFirstSnapshotForEntity(value);
        await Context.SaveChangesAsync();
    }

    public virtual async Task Update(TEntity value, DateTime timestamp)
    {
        if (IsTracked(value.Id, out var entity))
        {
            Context.Entry(entity).State = EntityState.Detached;
        }

        value.ModifiedOn = timestamp;

        Set.Update(value);
        await CreateSnapshot(value);
        await Context.SaveChangesAsync();
    }

    public virtual async Task Delete(TEntity value, DateTime timestamp)
    {
        if (IsTracked(value.Id, out var entity))
        {
            Context.Entry(entity).State = EntityState.Detached;
        }

        value.ModifiedOn = timestamp;

        Set.Remove(value);

        await CreateSnapshot(value, null, true);
        await Context.SaveChangesAsync();
    }

    public virtual async Task AddRange(IEnumerable<TEntity> values, DateTime timestamp)
    {
        foreach (var value in values)
        {
            value.CreatedOn = timestamp;
            value.ModifiedOn = timestamp;

            Set.Add(value);
            await CreateFirstSnapshotForEntity(value);
        }

        await Context.SaveChangesAsync();
    }

    public virtual async Task DeleteRange(IEnumerable<TEntity> values, DateTime timestamp)
    {
        var valueIds = values.Select(x => x.Id);
        var snapshotValues = await SnapshotSet
            .Where(x => valueIds.Contains(x.BasisId) && x.ValidTo == null)
            .ToListAsync();

        if (snapshotValues.Count != values.Count())
        {
            throw new ArgumentException("did not found all previous snapshots for the DeleteRange operation");
        }

        foreach (var value in values)
        {
            if (IsTracked(value.Id, out var entity))
            {
                Context.Entry(entity).State = EntityState.Detached;
            }

            value.ModifiedOn = timestamp;

            Set.Remove(value);
            await CreateSnapshot(value, snapshotValues, true);
        }

        await Context.SaveChangesAsync();
    }

    protected async Task CreateFirstSnapshotForEntity(TEntity entity)
    {
        await CreateSnapshot(entity, Enumerable.Empty<TSnapshotEntity>());
    }

    protected async Task CreateSnapshot(
        TEntity entity,
        IEnumerable<TSnapshotEntity>? snapshotItems = null,
        bool isDelete = false)
    {
        var snapshot = Mapper.Map<TSnapshotEntity>(entity);
        snapshot.Deleted = isDelete;

        var previousSnapshot = snapshotItems == null
            ? await SnapshotIncludeQuery(SnapshotSet).SingleOrDefaultAsync(x => x.BasisId == entity.Id && x.ValidTo == null)
            : snapshotItems.SingleOrDefault(x => x.BasisId == entity.Id && x.ValidTo == null);

        if (previousSnapshot != null)
        {
            previousSnapshot.ValidTo = entity.ModifiedOn;
            SnapshotSet.Update(previousSnapshot);
        }

        snapshot.ValidFrom = entity.ModifiedOn;
        SnapshotSet.Add(snapshot);
    }

    // Gets called on CreateSnapshot(). Necessary when a Snapshot has child entities, otherwise the Update PreviousSnapshot could fail
    // ex: It could fail when we call default constructors for child entities in the principal entity.
    protected virtual IQueryable<TSnapshotEntity> SnapshotIncludeQuery(IQueryable<TSnapshotEntity> query)
    {
        return query;
    }
}
