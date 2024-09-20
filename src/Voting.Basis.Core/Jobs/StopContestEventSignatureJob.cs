// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading;
using System.Threading.Tasks;
using Voting.Basis.Core.EventSignature;
using Voting.Basis.EventSignature;
using Voting.Lib.Common;
using Voting.Lib.Scheduler;

namespace Voting.Basis.Core.Jobs;

public class StopContestEventSignatureJob : IScheduledJob
{
    private readonly ContestCache _contestCache;
    private readonly IClock _clock;
    private readonly EventSignatureService _eventSignatureService;

    public StopContestEventSignatureJob(ContestCache contestCache, IClock clock, EventSignatureService eventSignatureService)
    {
        _contestCache = contestCache;
        _clock = clock;
        _eventSignatureService = eventSignatureService;
    }

    public async Task Run(CancellationToken ct)
    {
        using var batchWrite = _contestCache.BatchWrite();

        foreach (var entry in _contestCache.GetAll())
        {
            if (entry.KeyData == null || entry.KeyData.ValidTo >= _clock.UtcNow)
            {
                continue;
            }

            await _eventSignatureService.StopSignature(entry.Id, entry.KeyData);
            entry.KeyData.Key.Dispose();
            entry.KeyData = null;
        }
    }
}
