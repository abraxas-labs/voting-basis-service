﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Services;

namespace Voting.Basis.Core.Services.Write;

public class CantonSettingsWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly ITenantService _tenantService;

    public CantonSettingsWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ITenantService tenantService)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _tenantService = tenantService;
    }

    public async Task Create(CantonSettings data)
    {
        await SetAuthorityTenant(data);

        var cantonSettings = _aggregateFactory.New<CantonSettingsAggregate>();
        cantonSettings.CreateFrom(data);
        await _aggregateRepository.Save(cantonSettings);
    }

    public async Task Update(CantonSettings data)
    {
        await SetAuthorityTenant(data);
        var cantonSettings = await _aggregateRepository.GetById<CantonSettingsAggregate>(data.Id);
        cantonSettings.UpdateFrom(data);
        await _aggregateRepository.Save(cantonSettings);
    }

    private async Task SetAuthorityTenant(CantonSettings data)
    {
        var tenant = await _tenantService.GetTenant(data.SecureConnectId, true)
            ?? throw new ValidationException($"tenant with id {data.SecureConnectId} not found");
        data.AuthorityName = tenant.Name;
    }
}
