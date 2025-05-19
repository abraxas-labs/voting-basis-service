// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Ech.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class SecondaryMajorityElectionProcessor :
    IEventProcessor<SecondaryMajorityElectionCreated>,
    IEventProcessor<SecondaryMajorityElectionUpdated>,
    IEventProcessor<SecondaryMajorityElectionAfterTestingPhaseUpdated>,
    IEventProcessor<SecondaryMajorityElectionDeleted>,
    IEventProcessor<SecondaryMajorityElectionActiveStateUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateCreated>,
    IEventProcessor<SecondaryMajorityElectionCandidateUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateDeleted>,
    IEventProcessor<SecondaryMajorityElectionCandidatesReordered>,
    IEventProcessor<SecondaryMajorityElectionCandidateReferenceCreated>,
    IEventProcessor<SecondaryMajorityElectionCandidateReferenceUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateReferenceDeleted>
{
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _repo;
    private readonly SimplePoliticalBusinessBuilder<SecondaryMajorityElection> _simplePoliticalBusinessBuilder;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionCandidate> _candidateRepo;
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _majorityElectionCandidateRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly MajorityElectionBallotGroupEntryRepo _electionBallotGroupEntryRepo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;

    public SecondaryMajorityElectionProcessor(
        IDbRepository<DataContext, SecondaryMajorityElection> repo,
        IDbRepository<DataContext, SecondaryMajorityElectionCandidate> candidateRepo,
        IDbRepository<DataContext, MajorityElectionCandidate> majorityElectionCandidateRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        MajorityElectionBallotGroupEntryRepo electionBallotGroupEntryRepo,
        IMapper mapper,
        EventLoggerAdapter eventLogger,
        SimplePoliticalBusinessBuilder<SecondaryMajorityElection> simplePoliticalBusinessBuilder)
    {
        _repo = repo;
        _candidateRepo = candidateRepo;
        _majorityElectionCandidateRepo = majorityElectionCandidateRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _mapper = mapper;
        _eventLogger = eventLogger;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
        _electionBallotGroupEntryRepo = electionBallotGroupEntryRepo;
    }

    public async Task Process(SecondaryMajorityElectionCreated eventData)
    {
        var model = _mapper.Map<SecondaryMajorityElection>(eventData.SecondaryMajorityElection);

        var electionGroup = await _majorityElectionRepo.Query()
            .Where(me => me.Id == model.PrimaryMajorityElectionId)
            .Select(me => me.ElectionGroup)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(model.PrimaryMajorityElectionId);
        model.ElectionGroupId = electionGroup.Id;

        await _repo.Create(model);

        var electionInclPrimary = await GetElection(model.Id);
        await _simplePoliticalBusinessBuilder.Create(electionInclPrimary);
        await _eventLogger.LogSecondaryMajorityElectionEvent(eventData, electionInclPrimary);
    }

    public async Task Process(SecondaryMajorityElectionUpdated eventData)
    {
        var model = _mapper.Map<SecondaryMajorityElection>(eventData.SecondaryMajorityElection);
        var existingModel = await _repo.GetByKey(model.Id)
            ?? throw new EntityNotFoundException(model.Id);

        model.ElectionGroupId = existingModel.ElectionGroupId;
        await _repo.Update(model);

        if (existingModel.NumberOfMandates != model.NumberOfMandates)
        {
            await _electionBallotGroupEntryRepo.UpdateCandidateCountOk(model.Id, false, model.NumberOfMandates);
        }

        var electionInclPrimary = await GetElection(model.Id);

        await _simplePoliticalBusinessBuilder.Update(electionInclPrimary);
        await _eventLogger.LogSecondaryMajorityElectionEvent(eventData, electionInclPrimary);
    }

    public async Task Process(SecondaryMajorityElectionAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var election = await GetElection(id);

        // For backwards compability, we treat a missing political business number as no change to the field
        if (eventData.PoliticalBusinessNumber == string.Empty)
        {
            eventData.PoliticalBusinessNumber = election.PoliticalBusinessNumber;
        }

        _mapper.Map(eventData, election);

        await _repo.Update(election);
        await _simplePoliticalBusinessBuilder.Update(election);
        await _eventLogger.LogSecondaryMajorityElectionEvent(eventData, election);
    }

    public async Task Process(SecondaryMajorityElectionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var existingModel = await GetElection(id);

        await _repo.DeleteByKey(id);
        await _simplePoliticalBusinessBuilder.Delete(existingModel);
        await _eventLogger.LogSecondaryMajorityElectionEvent(eventData, existingModel);
    }

    public async Task Process(SecondaryMajorityElectionActiveStateUpdated eventData)
    {
        var smeId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var existingModel = await GetElection(smeId);

        existingModel.Active = eventData.Active;
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogSecondaryMajorityElectionEvent(eventData, existingModel);
    }

    public async Task Process(SecondaryMajorityElectionCandidateCreated eventData)
    {
        var model = _mapper.Map<SecondaryMajorityElectionCandidate>(eventData.SecondaryMajorityElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        await _candidateRepo.Create(model);

        await _eventLogger.LogSecondaryMajorityElectionCandidateEvent(eventData, await GetCandidate(model.Id));
    }

    public async Task Process(SecondaryMajorityElectionCandidateUpdated eventData)
    {
        var model = _mapper.Map<SecondaryMajorityElectionCandidate>(eventData.SecondaryMajorityElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        var existingModel = await GetCandidate(model.Id);

        await _candidateRepo.Update(model);

        await _eventLogger.LogSecondaryMajorityElectionCandidateEvent(
            eventData,
            model,
            existingModel.SecondaryMajorityElection.PrimaryMajorityElectionId,
            existingModel.SecondaryMajorityElection.ContestId,
            existingModel.SecondaryMajorityElection.DomainOfInfluenceId);
    }

    public async Task Process(SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var candidate = await GetCandidate(id);
        _mapper.Map(eventData, candidate);

        await _candidateRepo.Update(candidate);
        await _eventLogger.LogSecondaryMajorityElectionCandidateEvent(eventData, candidate);
    }

    public async Task Process(SecondaryMajorityElectionCandidateDeleted eventData)
    {
        await DeleteCandidate(eventData, eventData.SecondaryMajorityElectionCandidateId);
    }

    public async Task Process(SecondaryMajorityElectionCandidatesReordered eventData)
    {
        var secondaryMajorityElectionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var secondaryMajorityElection = await _repo.Query()
            .Include(sme => sme.Candidates)
            .Include(sme => sme.PrimaryMajorityElection)
            .FirstOrDefaultAsync(sme => sme.Id == secondaryMajorityElectionId)
            ?? throw new EntityNotFoundException(secondaryMajorityElectionId);

        var grouped = eventData.CandidateOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Single().Position);

        foreach (var candidate in secondaryMajorityElection.Candidates)
        {
            candidate.Position = grouped[candidate.Id];
        }

        await _candidateRepo.UpdateRange(secondaryMajorityElection.Candidates);
        await _eventLogger.LogSecondaryMajorityElectionEvent(eventData, secondaryMajorityElection);
    }

    public async Task Process(SecondaryMajorityElectionCandidateReferenceCreated eventData)
    {
        var referencedCandidateId = GuidParser.Parse(eventData.MajorityElectionCandidateReference.CandidateId);
        var referencedCandidate = await _majorityElectionCandidateRepo.GetByKey(referencedCandidateId);

        var candidateReference = _mapper.Map<SecondaryMajorityElectionCandidate>(referencedCandidate);
        _mapper.Map(eventData.MajorityElectionCandidateReference, candidateReference);

        await _candidateRepo.Create(candidateReference);
        await _eventLogger.LogSecondaryMajorityElectionCandidateEvent(eventData, await GetCandidate(candidateReference.Id));
    }

    public async Task Process(SecondaryMajorityElectionCandidateReferenceUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionCandidateReference.Id);
        var existingCandidate = await GetCandidate(id);

        existingCandidate.Incumbent = eventData.MajorityElectionCandidateReference.Incumbent;
        existingCandidate.Number = eventData.MajorityElectionCandidateReference.Number;
        existingCandidate.CheckDigit = eventData.MajorityElectionCandidateReference.CheckDigit;
        await _candidateRepo.Update(existingCandidate);
        await _eventLogger.LogSecondaryMajorityElectionCandidateEvent(eventData, existingCandidate);
    }

    public async Task Process(SecondaryMajorityElectionCandidateReferenceDeleted eventData)
    {
        await DeleteCandidate(eventData, eventData.SecondaryMajorityElectionCandidateReferenceId);
    }

    private async Task DeleteCandidate<T>(T eventData, string candidateId)
        where T : IMessage<T>
    {
        var id = GuidParser.Parse(candidateId);
        var existingCandidate = await GetCandidate(id);

        await _candidateRepo.DeleteByKey(id);

        var candidatesToUpdate = await _candidateRepo.Query()
            .Where(c => c.SecondaryMajorityElectionId == existingCandidate.SecondaryMajorityElectionId
                && c.Position > existingCandidate.Position)
            .ToListAsync();
        foreach (var candidate in candidatesToUpdate)
        {
            candidate.Position--;
        }

        await _candidateRepo.UpdateRange(candidatesToUpdate);
        await _eventLogger.LogSecondaryMajorityElectionCandidateEvent(eventData, existingCandidate);
    }

    private async Task<SecondaryMajorityElection> GetElection(Guid id)
    {
        return await _repo.Query()
            .Include(sme => sme.PrimaryMajorityElection)
            .Include(sme => sme.ElectionGroup)
            .FirstOrDefaultAsync(sme => sme.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task<SecondaryMajorityElectionCandidate> GetCandidate(Guid candidateId)
    {
        return await _candidateRepo.Query()
            .Include(c => c.SecondaryMajorityElection.PrimaryMajorityElection)
            .FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new EntityNotFoundException(candidateId);
    }

    private void TruncateCandidateNumber(SecondaryMajorityElectionCandidate candidate)
    {
        if (candidate.Number.Length <= 10)
        {
            return;
        }

        // old events can contain a number which is longer than 10 chars
        candidate.Number = candidate.Number[..10];
    }
}
