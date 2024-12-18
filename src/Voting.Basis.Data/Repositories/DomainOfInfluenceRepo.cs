﻿// (c) Copyright by Abraxas Informatik AG
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

        return (await Context.DomainOfInfluences.FromSqlRaw(
            $@"
                WITH RECURSIVE parents_or_self AS (
                    SELECT {idColumnName}, {parentIdColumnName}, {cantonColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}, x.{parentIdColumnName}, x.{cantonColumnName}
                    FROM {DelimitedSchemaAndTableName} x
                    JOIN parents_or_self p ON x.{idColumnName} = p.{parentIdColumnName}
                )
                SELECT * FROM parents_or_self
                WHERE {parentIdColumnName} IS NULL",
            domainOfInfluenceId)
            .Select(doi => doi.Canton)
            .ToListAsync()).FirstOrDefault();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<List<Guid>> GetHierarchicalLowerOrSelfDomainOfInfluenceIds(Guid domainOfInfluenceId)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);

        return await Context.DomainOfInfluences.FromSqlRaw(
                $@"
                WITH RECURSIVE children_or_self AS (
                    SELECT {idColumnName}, {parentIdColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}, x.{parentIdColumnName}
                    FROM {DelimitedSchemaAndTableName} x
                    JOIN children_or_self c ON x.{parentIdColumnName} = c.{idColumnName}
                )
                SELECT * FROM children_or_self",
                domainOfInfluenceId)
            .Select(doi => doi.Id)
            .ToListAsync();
    }
}
