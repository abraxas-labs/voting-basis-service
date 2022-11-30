// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.EventSignature;
using Voting.Basis.EventSignature;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Subscribe;

namespace Voting.Basis.Core.EventProcessors.Transient;

public class TransientCatchUpContestProcessor :
    ITransientCatchUpDetectorEventProcessor<ContestDeleted>,
    ITransientCatchUpDetectorEventProcessor<ContestArchived>
{
    private readonly ContestCache _contestCache;
    private readonly EventSignatureService _eventSignatureService;
    private readonly ILogger<TransientCatchUpContestProcessor> _logger;

    public TransientCatchUpContestProcessor(ContestCache contestCache, EventSignatureService eventSignatureService, ILogger<TransientCatchUpContestProcessor> logger)
    {
        _contestCache = contestCache;
        _eventSignatureService = eventSignatureService;
        _logger = logger;
    }

    public async Task Process(ContestDeleted eventData, bool isCatchUp)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);

        if (!isCatchUp)
        {
            await StopSignature(contestId);
        }

        _contestCache.Remove(contestId);
    }

    public async Task Process(ContestArchived eventData, bool isCatchUp)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);

        if (!isCatchUp)
        {
            await StopSignature(contestId);
        }

        _contestCache.Remove(contestId);
    }

    private async Task StopSignature(Guid contestId)
    {
        using var cacheWrite = _contestCache.BatchWrite();

        var entry = _contestCache.Get(contestId);
        if (entry?.KeyData != null)
        {
            await _eventSignatureService.StopSignature(contestId, entry.KeyData);
            entry.KeyData.Key.Dispose();
            entry.KeyData = null;
        }
        else
        {
            _logger.LogInformation("Transient catch up contest {ContestId} has no key assigned. No key delete needed.", contestId);
        }
    }
}
