// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.Jobs;

internal class ArchivePoliticalAssemblyJob : PoliticalAssemblyStateJob
{
    private readonly PoliticalAssemblyWriter _politicalAssemblyWriter;

    public ArchivePoliticalAssemblyJob(
        ILogger<ArchivePoliticalAssemblyJob> logger,
        IDbRepository<DataContext, PoliticalAssembly> politicalAssemblyRepo,
        IClock clock,
        PermissionService permissionService,
        PoliticalAssemblyWriter politicalAssemblyWriter)
        : base(logger, politicalAssemblyRepo, clock, permissionService)
    {
        _politicalAssemblyWriter = politicalAssemblyWriter;
    }

    protected override IQueryable<PoliticalAssembly> BuildQuery(IQueryable<PoliticalAssembly> query, DateTime now)
        => query.Where(c => c.ArchivePer < now && c.State == PoliticalAssemblyState.PastLocked);

    protected override Task<bool> SetPoliticalAssemblyState(Guid politicalAssemblyId)
        => _politicalAssemblyWriter.TryArchive(politicalAssemblyId);
}
