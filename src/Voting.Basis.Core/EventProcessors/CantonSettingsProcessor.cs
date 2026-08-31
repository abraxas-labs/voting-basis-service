// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;

namespace Voting.Basis.Core.EventProcessors;

public class CantonSettingsProcessor :
    IEventProcessor<CantonSettingsCreated>,
    IEventProcessor<CantonSettingsUpdated>,
    IEventProcessor<SetCantonSettingsECountingCertificateChain>
{
    private readonly CantonSettingsRepo _repo;
    private readonly IMapper _mapper;
    private readonly DomainOfInfluenceCantonDefaultsBuilder _cantonDefaultsBuilder;
    private readonly DataContext _dbContext;
    private readonly IClock _clock;

    public CantonSettingsProcessor(
        CantonSettingsRepo repo,
        IMapper mapper,
        DomainOfInfluenceCantonDefaultsBuilder cantonDefaultsBuilder,
        DataContext dbContext,
        IClock clock)
    {
        _repo = repo;
        _mapper = mapper;
        _cantonDefaultsBuilder = cantonDefaultsBuilder;
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task Process(CantonSettingsCreated eventData)
    {
        var model = _mapper.Map<CantonSettings>(eventData.CantonSettings);
        Migrate(model);

        await _repo.Create(model);
        await _cantonDefaultsBuilder.RebuildForCanton(model);
    }

    public async Task Process(CantonSettingsUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.CantonSettings.Id);
        var existing = await _repo.Query()
            .AsSplitQuery()
            .AsTracking()
            .Include(x => x.EnabledVotingCardChannels)
            .Include(x => x.CountingCircleResultStateDescriptions)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(nameof(CantonSettings), id);
        _mapper.Map(eventData.CantonSettings, existing);
        Migrate(existing);

        await _dbContext.SaveChangesAsync();
        await _cantonDefaultsBuilder.RebuildForCanton(existing);
    }

    public async Task Process(SetCantonSettingsECountingCertificateChain eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var rawPem = eventData.RawPem;

        var existingCertificateChain = await _dbContext.CantonSettingsECountingCertificateChains
            .FirstOrDefaultAsync(x => x.CantonSettingsId == id);

        if (existingCertificateChain != null)
        {
            _dbContext.CantonSettingsECountingCertificateChains.Remove(existingCertificateChain);
        }

        var certificateChain = CertificateChainBuilder.Build<CantonSettingsECountingCertificateChain, CantonSettingsECountingCertificateInfo>(rawPem, eventData.EventInfo.User.ToDataUser(), _clock.UtcNow);
        certificateChain.CantonSettingsId = id;

        _dbContext.CantonSettingsECountingCertificateChains.Add(certificateChain);
        await _dbContext.SaveChangesAsync();
    }

    private void Migrate(CantonSettings model)
    {
        // Set default sort type value since the old eventData (before introducing the sort type) can contain the unspecified value.
        if (model.ProtocolCountingCircleSortType == ProtocolCountingCircleSortType.Unspecified)
        {
            model.ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.SortNumber;
        }

        // Set default sort type value since the old eventData (before introducing the sort type) can contain the unspecified value.
        if (model.ProtocolDomainOfInfluenceSortType == ProtocolDomainOfInfluenceSortType.Unspecified)
        {
            model.ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.SortNumber;
        }
    }
}
