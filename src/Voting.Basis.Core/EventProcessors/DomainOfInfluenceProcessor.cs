// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Extensions;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class DomainOfInfluenceProcessor :
    IEventProcessor<DomainOfInfluenceCreated>,
    IEventProcessor<DomainOfInfluenceUpdated>,
    IEventProcessor<DomainOfInfluenceDeleted>,
    IEventProcessor<DomainOfInfluenceCountingCircleEntriesUpdated>,
    IEventProcessor<DomainOfInfluenceContactPersonUpdated>,
    IEventProcessor<DomainOfInfluenceVotingCardDataUpdated>,
    IEventProcessor<DomainOfInfluenceLogoDeleted>,
    IEventProcessor<DomainOfInfluenceLogoUpdated>,
    IEventProcessor<DomainOfInfluencePartyCreated>,
    IEventProcessor<DomainOfInfluencePartyUpdated>,
    IEventProcessor<DomainOfInfluencePartyDeleted>,
    IEventProcessor<DomainOfInfluencePlausibilisationConfigurationUpdated>
{
    private readonly DomainOfInfluenceRepo _repo;
    private readonly DomainOfInfluenceCountingCircleRepo _doiCcRepo;
    private readonly DataContext _dataContext;
    private readonly IDbRepository<DataContext, PlausibilisationConfiguration> _plausiRepo;
    private readonly DomainOfInfluencePermissionBuilder _permissionBuilder;
    private readonly DomainOfInfluenceHierarchyBuilder _hierarchyBuilder;
    private readonly DomainOfInfluenceCountingCircleInheritanceBuilder _doiCcInheritanceBuilder;
    private readonly DomainOfInfluenceCantonDefaultsBuilder _domainOfInfluenceCantonDefaultsBuilder;
    private readonly IDbRepository<DataContext, DomainOfInfluenceParty> _doiPartyRepo;
    private readonly DomainOfInfluenceHierarchyRepo _hierarchyRepo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;

    public DomainOfInfluenceProcessor(
        IMapper mapper,
        DomainOfInfluenceRepo repo,
        DomainOfInfluencePermissionBuilder permissionBuilder,
        DomainOfInfluenceHierarchyBuilder hierarchyBuilder,
        DomainOfInfluenceCountingCircleInheritanceBuilder doiCcInheritanceBuilder,
        DomainOfInfluenceCantonDefaultsBuilder domainOfInfluenceCantonDefaultsBuilder,
        IDbRepository<DataContext, DomainOfInfluenceParty> doiPartyRepo,
        DomainOfInfluenceHierarchyRepo hierarchyRepo,
        EventLoggerAdapter eventLogger,
        IDbRepository<DataContext, PlausibilisationConfiguration> plausiRepo,
        DomainOfInfluenceCountingCircleRepo doiCcRepo,
        DataContext dataContext)
    {
        _mapper = mapper;
        _repo = repo;
        _permissionBuilder = permissionBuilder;
        _hierarchyBuilder = hierarchyBuilder;
        _doiCcInheritanceBuilder = doiCcInheritanceBuilder;
        _eventLogger = eventLogger;
        _domainOfInfluenceCantonDefaultsBuilder = domainOfInfluenceCantonDefaultsBuilder;
        _doiPartyRepo = doiPartyRepo;
        _hierarchyRepo = hierarchyRepo;
        _plausiRepo = plausiRepo;
        _doiCcRepo = doiCcRepo;
        _dataContext = dataContext;
    }

    public async Task Process(DomainOfInfluenceCreated eventData)
    {
        var model = _mapper.Map<DomainOfInfluence>(eventData.DomainOfInfluence);
        await _domainOfInfluenceCantonDefaultsBuilder.BuildForDomainOfInfluence(model);
        await _repo.Create(model, eventData.EventInfo.Timestamp.ToDateTime());

        await _hierarchyBuilder.InsertDomainOfInfluence(model);
        await _permissionBuilder.BuildPermissionTreeForNewDomainOfInfluence(model);

        await _eventLogger.LogDomainOfInfluenceEvent(eventData, model);
    }

    public async Task Process(DomainOfInfluenceUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluence.Id);
        var model = _mapper.Map<DomainOfInfluence>(eventData.DomainOfInfluence);

        var existing = await _repo.Query()
            .Include(x => x.CountingCircles)
            .Include(x => x.PlausibilisationConfiguration)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        var rebuildForRootDoiCantonUpdate = model.ParentId == null && existing.Canton != model.Canton;
        var oldTenantId = existing.SecureConnectId;
        var tenantChanged = model.SecureConnectId != oldTenantId;

        var oldCanton = existing.Canton;
        _mapper.Map(eventData.DomainOfInfluence, existing);

        // do not unset the canton for non-root doi
        // all events for non-root doi's have the canton unspecified
        // as it is inherited from the root doi
        if (existing.Canton == DomainOfInfluenceCanton.Unspecified)
        {
            existing.Canton = oldCanton;
        }

        await _repo.Update(existing, eventData.EventInfo.Timestamp.ToDateTime());

        if (rebuildForRootDoiCantonUpdate)
        {
            await _domainOfInfluenceCantonDefaultsBuilder.RebuildForRootDomainOfInfluenceCantonUpdate(existing);
        }

        if (tenantChanged)
        {
            await _hierarchyBuilder.UpdateDomainOfInfluence(model.Id, model.SecureConnectId, oldTenantId);
            await _permissionBuilder.RebuildPermissionTreeForDomainOfInfluence(model.Id, [model.SecureConnectId, oldTenantId]);
        }

        await _eventLogger.LogDomainOfInfluenceEvent(eventData, existing);
    }

    public async Task Process(DomainOfInfluenceCountingCircleEntriesUpdated eventData)
    {
        var domainOfInfluenceId = GuidParser.Parse(eventData.DomainOfInfluenceCountingCircleEntries.Id);

        var countingCircleIds = eventData.DomainOfInfluenceCountingCircleEntries.CountingCircleIds
            .Select(GuidParser.Parse)
            .ToList();

        var nonInheritedCountingCircleIds = await _doiCcRepo.Query()
            .Where(x => x.DomainOfInfluenceId == domainOfInfluenceId)
            .WhereIsNotInherited()
            .Select(x => x.CountingCircleId)
            .ToListAsync();

        var countingCircleIdsToRemove = nonInheritedCountingCircleIds.Except(countingCircleIds).ToList();
        var countingCircleIdsToAdd = countingCircleIds.Except(nonInheritedCountingCircleIds).ToList();

        var dateTime = eventData.EventInfo.Timestamp.ToDateTime();
        var hierarchicalGreaterOrSelfDoiIds = await _hierarchyRepo.GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(domainOfInfluenceId);
        await _doiCcInheritanceBuilder.BuildInheritanceForCountingCircles(
            domainOfInfluenceId,
            hierarchicalGreaterOrSelfDoiIds,
            countingCircleIdsToAdd,
            countingCircleIdsToRemove,
            dateTime);

        await _permissionBuilder.RebuildPermissionTreeForDomainOfInfluence(domainOfInfluenceId);
    }

    public async Task Process(DomainOfInfluenceDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);

        // This needs to be kept for backwards compatibility, as in earlier versions deleting a DoI
        // would delete all children and references without an explicit event.
        // Now related entities are deleted first via an explicit event before the DoI is deleted.
        var hierarchyEntry = await _hierarchyRepo.Query()
            .FirstOrDefaultAsync(x => x.DomainOfInfluenceId == id)
            ?? throw new EntityNotFoundException(id);
        var doiIdsToRemove = hierarchyEntry.ChildIds.Prepend(id).ToList();
        var existingCcEntries = await _doiCcRepo.Query()
            .Where(doiCc => doiIdsToRemove.Contains(doiCc.SourceDomainOfInfluenceId))
            .ToListAsync();

        await _doiCcRepo.DeleteRange(existingCcEntries, eventData.EventInfo.Timestamp.ToDateTime());
        await _dataContext.ExportConfigurations
            .Where(x => doiIdsToRemove.Contains(x.DomainOfInfluenceId))
            .ExecuteDeleteAsync();
        await _dataContext.Contests
            .Where(x => x.State == ContestState.TestingPhase && doiIdsToRemove.Contains(x.DomainOfInfluenceId))
            .ExecuteDeleteAsync();
        await _dataContext.SimplePoliticalBusiness
            .Where(x => x.Contest.State == ContestState.TestingPhase && doiIdsToRemove.Contains(x.DomainOfInfluenceId))
            .ExecuteDeleteAsync();
        await _dataContext.Votes
            .Where(x => x.Contest.State == ContestState.TestingPhase && doiIdsToRemove.Contains(x.DomainOfInfluenceId))
            .ExecuteDeleteAsync();
        await _dataContext.MajorityElections
            .Where(x => x.Contest.State == ContestState.TestingPhase && doiIdsToRemove.Contains(x.DomainOfInfluenceId))
            .ExecuteDeleteAsync();
        await _dataContext.ProportionalElections
            .Where(x => x.Contest.State == ContestState.TestingPhase && doiIdsToRemove.Contains(x.DomainOfInfluenceId))
            .ExecuteDeleteAsync();

        // ensures that it will create a delete snapshot for all childs of the deleted
        var doisToRemove = await _repo.Query()
            .Where(x => doiIdsToRemove.Contains(x.Id))
            .ToListAsync();
        await _repo.DeleteRange(doisToRemove, eventData.EventInfo.Timestamp.ToDateTime());

        await _hierarchyBuilder.RemoveDomainOfInfluences(doiIdsToRemove);
        await _permissionBuilder.RebuildPermissionTreeForDomainOfInfluence(hierarchyEntry);
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, id);
    }

    public async Task Process(DomainOfInfluenceContactPersonUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        var doi = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData.ContactPerson, doi.ContactPerson);
        await _repo.Update(doi, eventData.EventInfo.Timestamp.ToDateTime());
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, doi);
    }

    public async Task Process(DomainOfInfluenceVotingCardDataUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        var doi = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, doi);
        await _repo.Update(doi, eventData.EventInfo.Timestamp.ToDateTime());
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, doi);
    }

    public async Task Process(DomainOfInfluenceLogoDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        var doi = await _repo.GetByKey(id)
                  ?? throw new EntityNotFoundException(id);

        doi.LogoRef = null;

        await _repo.Update(doi, eventData.EventInfo.Timestamp.ToDateTime());
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, doi);
    }

    public async Task Process(DomainOfInfluenceLogoUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        var doi = await _repo.GetByKey(id)
                  ?? throw new EntityNotFoundException(id);

        doi.LogoRef = eventData.LogoRef;

        await _repo.Update(doi, eventData.EventInfo.Timestamp.ToDateTime());
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, doi);
    }

    public async Task Process(DomainOfInfluencePartyCreated eventData)
    {
        var model = _mapper.Map<DomainOfInfluenceParty>(eventData.Party);
        var doi = await _repo.GetByKey(model.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(model.DomainOfInfluenceId);

        await _doiPartyRepo.Create(model);
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, doi);
    }

    public async Task Process(DomainOfInfluencePartyUpdated eventData)
    {
        var model = _mapper.Map<DomainOfInfluenceParty>(eventData.Party);
        var existingParty = _doiPartyRepo
            .Query()
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefault(x => x.Id == model.Id)
            ?? throw new EntityNotFoundException(model.Id);

        await _doiPartyRepo.Update(model);
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, existingParty.DomainOfInfluence);
    }

    public async Task Process(DomainOfInfluencePartyDeleted eventData)
    {
        var partyId = GuidParser.Parse(eventData.Id);
        var existingParty = _doiPartyRepo
            .Query()
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefault(x => x.Id == partyId)
            ?? throw new EntityNotFoundException(partyId);

        existingParty.Deleted = true;
        await _doiPartyRepo.Update(existingParty);
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, existingParty.DomainOfInfluence);
    }

    public async Task Process(DomainOfInfluencePlausibilisationConfigurationUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        var existing = await _repo.Query()
            .Include(x => x.PlausibilisationConfiguration)
            .Include(x => x.CountingCircles)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        await MapAndReplacePlausibilisationConfiguration(
            existing,
            eventData.PlausibilisationConfiguration,
            existing.PlausibilisationConfiguration?.Id);

        await _repo.Update(existing, eventData.EventInfo.Timestamp.ToDateTime());
        await _eventLogger.LogDomainOfInfluenceEvent(eventData, existing);
    }

    private async Task MapAndReplacePlausibilisationConfiguration(
        DomainOfInfluence doi,
        PlausibilisationConfigurationEventData? plausiConfig,
        Guid? existingPlausiConfigId)
    {
        if (existingPlausiConfigId != null)
        {
            await _plausiRepo.DeleteByKey(existingPlausiConfigId.Value);
        }

        if (plausiConfig == null)
        {
            return;
        }

        doi.PlausibilisationConfiguration = _mapper.Map<PlausibilisationConfiguration>(plausiConfig);

        var ccEntries = plausiConfig.ComparisonCountOfVotersCountingCircleEntries;
        if (ccEntries?.Any() != true)
        {
            return;
        }

        var ccEntryByCcId = ccEntries.ToDictionary(x => x.CountingCircleId, x => x);

        foreach (var doiCc in doi.CountingCircles)
        {
            doiCc.ComparisonCountOfVotersCategory = ccEntryByCcId.TryGetValue(doiCc.CountingCircleId.ToString(), out var ccEntry)
                ? _mapper.Map<ComparisonCountOfVotersCategory>(ccEntry.Category)
                : ComparisonCountOfVotersCategory.Unspecified;
        }
    }
}
