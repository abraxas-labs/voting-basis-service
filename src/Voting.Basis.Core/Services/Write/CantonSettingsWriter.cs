// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.Utils;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Services;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Write;

public class CantonSettingsWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly ITenantService _tenantService;
    private readonly IClock _clock;
    private readonly IAuth _auth;

    public CantonSettingsWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ITenantService tenantService,
        IClock clock,
        IAuth auth)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _tenantService = tenantService;
        _clock = clock;
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
        cantonSettings.UpdateFrom(data);
        await _aggregateRepository.Save(cantonSettings);
    }

    public async Task<Data.Models.CantonSettingsECountingCertificateChain> SetECountingCertificateChain(Guid id, string rawPemChain)
    {
        // Ensures that the pem has the right format and certificates.
        var certChain = CertificateChainBuilder.Build<Data.Models.CantonSettingsECountingCertificateChain, Data.Models.CantonSettingsECountingCertificateInfo>(
            rawPemChain,
            _auth.User.ToDataUser(),
            _clock.UtcNow);

        var cantonSettings = await _aggregateRepository.GetById<CantonSettingsAggregate>(id);
        cantonSettings.SetECountingCertificateChain(rawPemChain);
        await _aggregateRepository.Save(cantonSettings);
        certChain.Infos = certChain.Infos!.OrderBy(i => i.NotAfter).ThenBy(i => i.Subject).ToList();
        return certChain;
    }

    private async Task SetAuthorityTenant(CantonSettings data)
    {
        var tenant = await _tenantService.GetTenant(data.SecureConnectId, true)
            ?? throw new ValidationException($"tenant with id {data.SecureConnectId} not found");
        data.AuthorityName = tenant.Name;
    }
}
