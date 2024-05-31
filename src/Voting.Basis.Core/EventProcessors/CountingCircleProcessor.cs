// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class CountingCircleProcessor :
    IEventProcessor<CountingCircleCreated>,
    IEventProcessor<CountingCircleDeleted>,
    IEventProcessor<CountingCircleUpdated>,
    IEventProcessor<CountingCircleMerged>,
    IEventProcessor<CountingCirclesMergerScheduled>,
    IEventProcessor<CountingCirclesMergerScheduleUpdated>,
    IEventProcessor<CountingCirclesMergerActivated>,
    IEventProcessor<CountingCirclesMergerScheduleDeleted>
{
    private readonly CountingCircleRepo _repo;
    private readonly IDbRepository<DataContext, CountingCirclesMerger> _ccMergeRepo;
    private readonly DomainOfInfluenceCountingCircleRepo _doiCcRepo;
    private readonly DomainOfInfluencePermissionBuilder _permissionBuilder;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;
    private readonly IDbRepository<DataContext, CountingCircleElectorate> _electorateRepo;

    public CountingCircleProcessor(
        CountingCircleRepo repo,
        IDbRepository<DataContext, CountingCirclesMerger> ccMergeRepo,
        DomainOfInfluenceCountingCircleRepo doiCcRepo,
        DomainOfInfluencePermissionBuilder permissionBuilder,
        IMapper mapper,
        EventLoggerAdapter eventLogger,
        IDbRepository<DataContext, CountingCircleElectorate> electorateRepo)
    {
        _repo = repo;
        _ccMergeRepo = ccMergeRepo;
        _doiCcRepo = doiCcRepo;
        _mapper = mapper;
        _permissionBuilder = permissionBuilder;
        _eventLogger = eventLogger;
        _electorateRepo = electorateRepo;
    }

    public async Task Process(CountingCircleCreated eventData)
    {
        var model = _mapper.Map<CountingCircle>(eventData.CountingCircle);
        await _repo.Create(model, eventData.EventInfo.Timestamp.ToDateTime());
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, model);
    }

    public async Task Process(CountingCircleUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.CountingCircle.Id);
        var existing = await _repo.Query()
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.Electorates)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        var model = _mapper.Map<CountingCircle>(eventData.CountingCircle);
        model.ResponsibleAuthority.Id = existing.ResponsibleAuthority.Id;
        model.ContactPersonDuringEvent.Id = existing.ContactPersonDuringEvent.Id;
        model.CreatedOn = existing.CreatedOn;

        if (model.ContactPersonAfterEvent != null)
        {
            model.ContactPersonAfterEvent.Id = existing.ContactPersonAfterEvent?.Id ?? Guid.Empty;
        }

        await ReplaceElectorates(existing, model.Electorates.ToList());
        model.Electorates = null!;

        await _repo.Update(model, eventData.EventInfo.Timestamp.ToDateTime());
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, model);
    }

    public async Task Process(CountingCircleDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.CountingCircleId);
        var existing = await _repo.Query()
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ResponsibleAuthority)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        existing.State = CountingCircleState.Deleted;
        await _repo.Delete(existing, eventData.EventInfo.Timestamp.ToDateTime());
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, existing);
    }

    public async Task Process(CountingCirclesMergerScheduled eventData)
    {
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var mergedCcIds = eventData.Merger.MergedCountingCircleIds.Select(GuidParser.Parse).ToHashSet();
        var copyFromCcId = GuidParser.Parse(eventData.Merger.CopyFromCountingCircleId);
        var newCcId = GuidParser.Parse(eventData.Merger.NewCountingCircle.Id);

        var mergedCcs = await _repo.Query()
            .Where(x => mergedCcIds.Contains(x.Id))
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.DomainOfInfluences)
            .ToListAsync();
        var copyFromCc = mergedCcs.Find(x => x.Id == copyFromCcId)
            ?? throw new EntityNotFoundException(copyFromCcId);

        var mergeModel = _mapper.Map<CountingCirclesMerger>(eventData.Merger);
        var newCountingCircle = mergeModel.NewCountingCircle!;
        newCountingCircle.State = CountingCircleState.Inactive;

        // copy domain of influence counting circles
        var newDoiCcs = copyFromCc.DomainOfInfluences.Select(doiCc => new DomainOfInfluenceCountingCircle
        {
            Inherited = doiCc.Inherited,
            DomainOfInfluenceId = doiCc.DomainOfInfluenceId,
            CountingCircleId = newCcId,
        }).ToList();

        await _repo.Create(newCountingCircle, timestamp);
        await _doiCcRepo.AddRange(newDoiCcs, timestamp);
        await _ccMergeRepo.Create(mergeModel);

        foreach (var mergedCc in mergedCcs)
        {
            mergedCc.MergeTargetId = mergeModel.Id;
        }

        await _repo.UpdateRange(mergedCcs);
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, newCountingCircle);
    }

    public async Task Process(CountingCirclesMergerScheduleUpdated eventData)
    {
        var updatedMergedCcIds = eventData.Merger.MergedCountingCircleIds.Select(GuidParser.Parse).ToList();
        var copyFromCcId = GuidParser.Parse(eventData.Merger.CopyFromCountingCircleId);
        var mergerId = GuidParser.Parse(eventData.Merger.Id);

        var merger = await _ccMergeRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .IgnoreQueryFilters() // ignore query filters, to load new cc which is not yet active.
            .Include(x => x.NewCountingCircle!.DomainOfInfluences)
            .Include(x => x.NewCountingCircle!.ContactPersonDuringEvent)
            .Include(x => x.NewCountingCircle!.ContactPersonAfterEvent)
            .Include(x => x.NewCountingCircle!.ResponsibleAuthority)
            .Include(x => x.MergedCountingCircles)
            .FirstOrDefaultAsync(x => x.Id == mergerId)
            ?? throw new EntityNotFoundException(nameof(CountingCirclesMerger), mergerId);

        var updatedMergedCcs = await _repo.Query()
            .AsTracking()
            .Where(x => updatedMergedCcIds.Contains(x.Id))
            .Include(x => x.DomainOfInfluences)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ResponsibleAuthority)
            .ToListAsync();
        var copyFromCc = updatedMergedCcs.Find(x => x.Id == copyFromCcId)
            ?? throw new EntityNotFoundException(copyFromCcId);

        _mapper.Map(eventData.Merger, merger);

        merger.MergedCountingCircles.Update(
            updatedMergedCcs,
            cc => cc.Id);
        merger.NewCountingCircle!.DomainOfInfluences.Update(
            copyFromCc.DomainOfInfluences,
            x => x.DomainOfInfluenceId,
            x => new DomainOfInfluenceCountingCircle
            {
                Inherited = x.Inherited,
                DomainOfInfluenceId = x.DomainOfInfluenceId,
                CountingCircleId = merger.NewCountingCircle.Id,
            });

        await _ccMergeRepo.Update(merger);
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, merger.NewCountingCircle);
    }

    public async Task Process(CountingCirclesMergerScheduleDeleted eventData)
    {
        var mergerId = GuidParser.Parse(eventData.MergerId);
        var newCcId = GuidParser.Parse(eventData.NewCountingCircleId);

        var newCountingCircle = await _repo.Query()
            .IgnoreQueryFilters() // cc is not active
            .Include(x => x.ResponsibleAuthority) // these includes are required for the snapshotting
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .FirstOrDefaultAsync(x => x.Id == newCcId)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), newCcId);

        newCountingCircle.State = CountingCircleState.Deleted;

        // however, we delete it the hard way (no soft delete), since it was never actively used/accessible.
        // the snapshots remain intact, but all entries of this counting circle have the state inactive
        // and therefore won't show up in history views.
        await _ccMergeRepo.DeleteByKey(mergerId);
        await _repo.HardDelete(newCountingCircle, eventData.EventInfo.Timestamp.ToDateTime());
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, newCountingCircle);
    }

    public async Task Process(CountingCirclesMergerActivated eventData)
    {
        var timestamp = eventData.EventInfo.Timestamp.ToDateTime();
        var mergerId = GuidParser.Parse(eventData.Merger.Id);
        var merger = await _ccMergeRepo.Query()
            .IgnoreQueryFilters() // cc is inactive.
            .Include(x => x.NewCountingCircle!.ResponsibleAuthority) // these includes are required for the snapshotting
            .Include(x => x.NewCountingCircle!.ContactPersonDuringEvent)
            .Include(x => x.NewCountingCircle!.ContactPersonAfterEvent)
            .FirstOrDefaultAsync(x => x.Id == mergerId)
            ?? throw new EntityNotFoundException(nameof(CountingCirclesMerger), mergerId);

        merger.Merged = true;
        merger.NewCountingCircle!.State = CountingCircleState.Active;

        await _repo.Update(merger.NewCountingCircle, timestamp);
        await _ccMergeRepo.Update(merger);
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, merger.NewCountingCircle);
    }

    public async Task Process(CountingCircleMerged eventData)
    {
        var id = GuidParser.Parse(eventData.CountingCircleId);
        var existing = await _repo.Query()
           .Include(x => x.ResponsibleAuthority) // these includes are required for the snapshotting
           .Include(x => x.ContactPersonDuringEvent)
           .Include(x => x.ContactPersonAfterEvent)
           .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        existing.State = CountingCircleState.Merged;
        await _repo.Delete(existing, eventData.EventInfo.Timestamp.ToDateTime());
        await _permissionBuilder.RebuildPermissionTree();
        await _eventLogger.LogCountingCircleEvent(eventData, existing);
    }

    private async Task ReplaceElectorates(CountingCircle existing, IReadOnlyCollection<CountingCircleElectorate> electorates)
    {
        await _electorateRepo.DeleteRangeByKey(existing.Electorates.Select(e => e.Id).ToList());
        foreach (var electorate in electorates)
        {
            electorate.CountingCircleId = existing.Id;
        }

        await _electorateRepo.CreateRange(electorates);
    }
}
