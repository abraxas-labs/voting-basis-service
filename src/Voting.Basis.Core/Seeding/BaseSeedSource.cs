// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Threading.Tasks;
using Voting.Basis.Core.Services.Permission;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Seeding;

namespace Voting.Basis.Core.Seeding;

public abstract class BaseSeedSource<TAggregate> : IAggregateSeedSource
    where TAggregate : BaseEventSourcingAggregate
{
    private readonly string _seedFileName;
    private readonly PermissionService _permissionService;
    private readonly IAggregateFactory _aggregateFactory;

    protected BaseSeedSource(
        string seedFileName,
        PermissionService permissionService,
        IAggregateFactory aggregateFactory)
    {
        _seedFileName = seedFileName;
        _permissionService = permissionService;
        _aggregateFactory = aggregateFactory;
    }

    public async Task<BaseEventSourcingAggregate> GetAggregate()
    {
        _permissionService.SetAbraxasAuthIfNotAuthenticated();

        var aggregate = _aggregateFactory.New<TAggregate>();
        var rawSeedData = await ReadSeedData();

        FillAggregate(aggregate, rawSeedData);

        return aggregate;
    }

    protected abstract void FillAggregate(TAggregate aggregate, string rawSeedData);

    private async Task<string> ReadSeedData()
    {
        var assemblyFolder = Path.GetDirectoryName(GetType().Assembly.Location);
        var path = Path.Join(assemblyFolder, $"Seeding/Data/{_seedFileName}");
        return await File.ReadAllTextAsync(path);
    }
}
