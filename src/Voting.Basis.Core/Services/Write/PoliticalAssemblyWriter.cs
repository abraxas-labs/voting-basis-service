// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
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
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;

    public PoliticalAssemblyWriter(
        ILogger<PoliticalAssemblyWriter> logger,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo)
    {
        _logger = logger;
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _doiRepo = doiRepo;
    }

    public async Task Create(PoliticalAssembly data)
    {
        if (data.Date == default)
        {
            throw new ValidationException("Date cannot be undefined");
        }

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(data.DomainOfInfluenceId);
        await EnsureDoiIsResponsibleForVotingCards(data.DomainOfInfluenceId);

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
        await EnsureDoiIsResponsibleForVotingCards(data.DomainOfInfluenceId);

        politicalAssembly.UpdateFrom(data);
        await _aggregateRepository.Save(politicalAssembly);
    }

    public async Task Delete(Guid politicalAssemblyId)
    {
        var politicalAssembly = await _aggregateRepository.GetById<PoliticalAssemblyAggregate>(politicalAssemblyId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(politicalAssembly.DomainOfInfluenceId);
        await EnsureDoiIsResponsibleForVotingCards(politicalAssembly.DomainOfInfluenceId);

        politicalAssembly.Delete();
        await _aggregateRepository.Save(politicalAssembly);
        _logger.LogInformation("Deleted political assembly {PoliticalAssemblyId}.", politicalAssemblyId);
    }

    private async Task EnsureDoiIsResponsibleForVotingCards(Guid domainOfInfluenceId)
    {
        var doi = await _doiRepo.GetByKey(domainOfInfluenceId)
                  ?? throw new EntityNotFoundException(domainOfInfluenceId);

        if (!doi.ResponsibleForVotingCards)
        {
            throw new ValidationException(
                "Cannot create, update or delete a political assembly for a domain of influence, which is not responsible for voting cards.");
        }
    }
}
