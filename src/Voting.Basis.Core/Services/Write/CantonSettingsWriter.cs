// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Services;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Write;

public class CantonSettingsWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly ITenantService _tenantService;
    private readonly IAuth _auth;

    public CantonSettingsWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ITenantService tenantService,
        IAuth auth)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _tenantService = tenantService;
        _auth = auth;
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

        if (!_auth.HasPermission(Permissions.CantonSettings.UpdateAll)
            && (data.SecureConnectId != _auth.Tenant.Id || cantonSettings.SecureConnectId != _auth.Tenant.Id))
        {
            throw new ForbiddenException("Not enough rights to update the canton settings");
        }

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
