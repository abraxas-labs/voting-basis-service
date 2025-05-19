// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Basis.Data.Repositories.Snapshot;

namespace Voting.Basis.Data.Repositories;

public class DomainOfInfluenceRepo : HasSnapshotDbRepository<DomainOfInfluence, DomainOfInfluenceSnapshot>
{
    public DomainOfInfluenceRepo(DataContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<DomainOfInfluenceCanton> GetRootCanton(Guid domainOfInfluenceId)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);
        var cantonColumnName = GetDelimitedColumnName(x => x.Canton);
        var deletedColumnName = GetDelimitedColumnName(x => x.Deleted);

        return (await Context.DomainOfInfluences
            .FromSqlRaw(
            $@"
                WITH RECURSIVE parents_or_self AS (
                    SELECT {idColumnName}, {parentIdColumnName}, {cantonColumnName}, {deletedColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}, x.{parentIdColumnName}, x.{cantonColumnName}, x.{deletedColumnName}
                    FROM {DelimitedSchemaAndTableName} x
                    JOIN parents_or_self p ON x.{idColumnName} = p.{parentIdColumnName}
                )
                SELECT * FROM parents_or_self
                WHERE {parentIdColumnName} IS NULL AND {deletedColumnName} = FALSE",
            domainOfInfluenceId)
            .IgnoreQueryFilters() // Deleted filtering is done manually
            .Select(doi => doi.Canton)
            .ToListAsync()).FirstOrDefault();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<List<Guid>> GetHierarchicalLowerOrSelfDomainOfInfluenceIds(Guid domainOfInfluenceId)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);
        var deletedColumnName = GetDelimitedColumnName(x => x.Deleted);

        return await Context.DomainOfInfluences.FromSqlRaw(
                $@"
                WITH RECURSIVE children_or_self AS (
                    SELECT {idColumnName}, {parentIdColumnName}, {deletedColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}, x.{parentIdColumnName}, x.{deletedColumnName}
                    FROM {DelimitedSchemaAndTableName} x
                    JOIN children_or_self c ON x.{parentIdColumnName} = c.{idColumnName}
                )
                SELECT * FROM children_or_self
                WHERE {deletedColumnName} = FALSE",
                domainOfInfluenceId)
            .IgnoreQueryFilters() // Deleted filtering is done manually
            .Select(doi => doi.Id)
            .ToListAsync();
    }

    public async Task<List<DomainOfInfluence>> GetAllSlim()
    {
        return await Query()
            .Select(x => new DomainOfInfluence
            {
                Id = x.Id,
                SecureConnectId = x.SecureConnectId,
                ParentId = x.ParentId,
            })
            .ToListAsync();
    }

    public override async Task DeleteRange(IEnumerable<DomainOfInfluence> values, DateTime timestamp)
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
            value.Deleted = true;

            Set.Update(value);
            await CreateSnapshot(value, snapshotValues, true);
        }

        await Context.SaveChangesAsync();
    }

    public override async Task Delete(DomainOfInfluence value, DateTime timestamp)
    {
        if (IsTracked(value.Id, out var entity))
        {
            Context.Entry(entity).State = EntityState.Detached;
        }

        value.ModifiedOn = timestamp;
        value.Deleted = true;
        Set.Update(value);

        await CreateSnapshot(value, null, true);
        await Context.SaveChangesAsync();
    }
}
