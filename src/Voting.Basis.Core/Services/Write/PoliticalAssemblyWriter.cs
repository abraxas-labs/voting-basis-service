// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Permission;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using PoliticalAssembly = Voting.Basis.Core.Domain.PoliticalAssembly;

namespace Voting.Basis.Core.Services.Write;

public class PoliticalAssemblyWriter
{
    private readonly ILogger<PoliticalAssemblyWriter> _logger;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;

    public PoliticalAssemblyWriter(
        ILogger<PoliticalAssemblyWriter> logger,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService)
    {
        _logger = logger;
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
    }

    public async Task Create(PoliticalAssembly data)
    {
        if (data.Date == default)
        {
            throw new ValidationException("Date cannot be undefined");
        }

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(data.DomainOfInfluenceId);

        var politicalAssemblyAggregate = _aggregateFactory.New<PoliticalAssemblyAggregate>();
        politicalAssemblyAggregate.CreateFrom(data);
        await _aggregateRepository.Save(politicalAssemblyAggregate);
    }

    public async Task Update(PoliticalAssembly data)
    {
        if (data.Date == default)
        {
            throw new ValidationException("Date cannot be null");
        }

        var politicalAssembly = await _aggregateRepository.GetById<PoliticalAssemblyAggregate>(data.Id);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(politicalAssembly.DomainOfInfluenceId);

        politicalAssembly.UpdateFrom(data);
        await _aggregateRepository.Save(politicalAssembly);
    }

    public async Task Delete(Guid politicalAssemblyId)
    {
        var politicalAssembly = await _aggregateRepository.GetById<PoliticalAssemblyAggregate>(politicalAssemblyId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(politicalAssembly.DomainOfInfluenceId);

        politicalAssembly.Delete();
        await _aggregateRepository.Save(politicalAssembly);
        _logger.LogInformation("Deleted political assembly {PoliticalAssemblyId}.", politicalAssemblyId);
    }

    public async Task Archive(Guid politicalAssemblyId, DateTime? archivePer)
    {
        var politicalAssembly = await _aggregateRepository.GetById<PoliticalAssemblyAggregate>(politicalAssemblyId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(politicalAssembly.DomainOfInfluenceId);

        politicalAssembly.Archive(archivePer);
        await _aggregateRepository.Save(politicalAssembly);
        _logger.LogInformation("Archived political assembly {PoliticalAssemblyId}.", politicalAssemblyId);
    }

    internal async Task<bool> TrySetPastLocked(Guid politicalAssemblyId)
    {
        var politicalAssembly = await _aggregateRepository.GetById<PoliticalAssemblyAggregate>(politicalAssemblyId);
        if (!politicalAssembly.TrySetPastLocked())
        {
            return false;
        }

        await _aggregateRepository.Save(politicalAssembly);
        _logger.LogInformation("Set past locked for political assembly {PoliticalAssemblyId}.", politicalAssemblyId);
        return true;
    }

    internal async Task<bool> TryArchive(Guid politicalAssemblyId)
    {
        var politicalAssembly = await _aggregateRepository.GetById<PoliticalAssemblyAggregate>(politicalAssemblyId);
        if (!politicalAssembly.TryArchive())
        {
            return false;
        }

        await _aggregateRepository.Save(politicalAssembly);
        _logger.LogInformation("Archived political assembly {PoliticalAssemblyId}.", politicalAssemblyId);
        return true;
    }
}
