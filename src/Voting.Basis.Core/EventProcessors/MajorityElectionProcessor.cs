// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Ech.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class MajorityElectionProcessor :
    IEventProcessor<MajorityElectionCreated>,
    IEventProcessor<MajorityElectionUpdated>,
    IEventProcessor<MajorityElectionAfterTestingPhaseUpdated>,
    IEventProcessor<MajorityElectionActiveStateUpdated>,
    IEventProcessor<MajorityElectionEVotingApprovalUpdated>,
    IEventProcessor<MajorityElectionDeleted>,
    IEventProcessor<MajorityElectionToNewContestMoved>,
    IEventProcessor<MajorityElectionCandidateCreated>,
    IEventProcessor<MajorityElectionCandidateUpdated>,
    IEventProcessor<MajorityElectionCandidateAfterTestingPhaseUpdated>,
    IEventProcessor<MajorityElectionCandidatesReordered>,
    IEventProcessor<MajorityElectionCandidateDeleted>
{
    private readonly IDbRepository<DataContext, MajorityElection> _repo;
    private readonly SimplePoliticalBusinessBuilder<MajorityElection> _simplePoliticalBusinessBuilder;
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _candidateRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionCandidate> _secondaryMajorityCandidateRepo;
    private readonly IMapper _mapper;
    private readonly MajorityElectionBallotGroupEntryRepo _electionBallotGroupEntryRepo;
    private readonly EventLoggerAdapter _eventLogger;
    private readonly ILogger<MajorityElectionProcessor> _logger;

    public MajorityElectionProcessor(
        ILogger<MajorityElectionProcessor> logger,
        IDbRepository<DataContext, MajorityElection> repo,
        IDbRepository<DataContext, MajorityElectionCandidate> candidateRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionCandidate> secondaryMajorityCandidateRepo,
        IMapper mapper,
        EventLoggerAdapter eventLogger,
        MajorityElectionBallotGroupEntryRepo electionBallotGroupEntryRepo,
        SimplePoliticalBusinessBuilder<MajorityElection> simplePoliticalBusinessBuilder)
    {
        _logger = logger;
        _repo = repo;
        _candidateRepo = candidateRepo;
        _secondaryMajorityCandidateRepo = secondaryMajorityCandidateRepo;
        _mapper = mapper;
        _eventLogger = eventLogger;
        _electionBallotGroupEntryRepo = electionBallotGroupEntryRepo;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
    }

    public async Task Process(MajorityElectionCreated eventData)
    {
        var model = _mapper.Map<MajorityElection>(eventData.MajorityElection);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (model.ReviewProcedure == MajorityElectionReviewProcedure.Unspecified)
        {
            model.ReviewProcedure = MajorityElectionReviewProcedure.Electronically;
        }

        await _repo.Create(model);
        await _simplePoliticalBusinessBuilder.Create(model);
        await _eventLogger.LogMajorityElectionEvent(eventData, model);
    }

    public async Task Process(MajorityElectionUpdated eventData)
    {
        var model = _mapper.Map<MajorityElection>(eventData.MajorityElection);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (model.ReviewProcedure == MajorityElectionReviewProcedure.Unspecified)
        {
            model.ReviewProcedure = MajorityElectionReviewProcedure.Electronically;
        }

        var existingModel = await GetMajorityElection(model.Id);
        model.ElectionGroup = existingModel.ElectionGroup;

        await _repo.Update(model);
        await _simplePoliticalBusinessBuilder.Update(model);

        if (model.NumberOfMandates != existingModel.NumberOfMandates)
        {
            await _electionBallotGroupEntryRepo.UpdateCandidateCountOk(model.Id, true, model.NumberOfMandates);
        }

        await _eventLogger.LogMajorityElectionEvent(eventData, model);
    }

    public async Task Process(MajorityElectionAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var election = await GetMajorityElection(id);
        _mapper.Map(eventData, election);

        await _repo.Update(election);
        await _simplePoliticalBusinessBuilder.Update(election);
        await _eventLogger.LogMajorityElectionEvent(eventData, election);
    }

    public async Task Process(MajorityElectionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionId);
        try
        {
            var existingModel = await GetMajorityElection(id);
            await _repo.DeleteByKey(id);
            await _simplePoliticalBusinessBuilder.Delete(existingModel);
            await _eventLogger.LogMajorityElectionEvent(eventData, existingModel);
        }
        catch (EntityNotFoundException)
        {
            // skip event processing to prevent race condition if majority election was deleted from other process.
            _logger.LogWarning("event 'MajorityElectionDeleted' skipped. majority election {id} has already been deleted", id);
        }
    }

    public async Task Process(MajorityElectionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionId);
        var existingModel = await GetMajorityElection(id);

        existingModel.ContestId = GuidParser.Parse(eventData.NewContestId);
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogMajorityElectionEvent(eventData, existingModel);
    }

    public async Task Process(MajorityElectionActiveStateUpdated eventData)
    {
        var majorityElectionId = GuidParser.Parse(eventData.MajorityElectionId);
        var existingModel = await GetMajorityElection(majorityElectionId);

        existingModel.Active = eventData.Active;
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogMajorityElectionEvent(eventData, existingModel);
    }

    public async Task Process(MajorityElectionEVotingApprovalUpdated eventData)
    {
        var majorityElectionId = GuidParser.Parse(eventData.MajorityElectionId);
        var existingModel = await GetMajorityElection(majorityElectionId);

        existingModel.EVotingApproved = eventData.Approved;
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogMajorityElectionEvent(eventData, existingModel);
    }

    public async Task Process(MajorityElectionCandidateCreated eventData)
    {
        var model = _mapper.Map<MajorityElectionCandidate>(eventData.MajorityElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        await _candidateRepo.Create(model);
        await _eventLogger.LogMajorityElectionCandidateEvent(eventData, await GetMajorityElectionCandidate(model.Id));
    }

    public async Task Process(MajorityElectionCandidateUpdated eventData)
    {
        var model = _mapper.Map<MajorityElectionCandidate>(eventData.MajorityElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        var existingModel = await GetMajorityElectionCandidate(model.Id);

        await _candidateRepo.Update(model);
        await UpdateCandidateReferences(model);

        await _eventLogger.LogMajorityElectionCandidateEvent(eventData, model, existingModel.MajorityElection.ContestId, existingModel.MajorityElection.DomainOfInfluenceId);
    }

    public async Task Process(MajorityElectionCandidateAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var candidate = await GetMajorityElectionCandidate(id);
        _mapper.Map(eventData, candidate);

        await _candidateRepo.Update(candidate);
        await UpdateCandidateReferences(candidate);

        await _eventLogger.LogMajorityElectionCandidateEvent(eventData, candidate, candidate.MajorityElection.ContestId, candidate.MajorityElection.DomainOfInfluenceId);
    }

    public async Task Process(MajorityElectionCandidatesReordered eventData)
    {
        var majorityElectionId = GuidParser.Parse(eventData.MajorityElectionId);
        var majorityElection = await _repo.Query()
            .Include(m => m.MajorityElectionCandidates)
            .FirstOrDefaultAsync(m => m.Id == majorityElectionId)
            ?? throw new EntityNotFoundException(majorityElectionId);

        var grouped = eventData.CandidateOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Single().Position);

        foreach (var candidate in majorityElection.MajorityElectionCandidates)
        {
            candidate.Position = grouped[candidate.Id];
        }

        await _candidateRepo.UpdateRange(majorityElection.MajorityElectionCandidates);
        await _eventLogger.LogMajorityElectionEvent(eventData, majorityElection);
    }

    public async Task Process(MajorityElectionCandidateDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionCandidateId);
        try
        {
            var existingCandidate = await GetMajorityElectionCandidate(id);
            await _candidateRepo.DeleteByKey(id);
            var candidatesToUpdate = await _candidateRepo.Query()
                .Where(c => c.MajorityElectionId == existingCandidate.MajorityElectionId
                    && c.Position > existingCandidate.Position)
                .ToListAsync();

            foreach (var candidate in candidatesToUpdate)
            {
                candidate.Position--;
            }

            await _candidateRepo.UpdateRange(candidatesToUpdate);
            await _eventLogger.LogMajorityElectionCandidateEvent(eventData, existingCandidate);
        }
        catch (EntityNotFoundException)
        {
            // skip event processing to prevent race condition if majority election candidate was deleted from other process.
            _logger.LogWarning("event 'MajorityElectionCandidateDeleted' skipped. majority election candidate {id} has already been deleted", id);
        }
    }

    private async Task<MajorityElection> GetMajorityElection(Guid id)
    {
        return await _repo.Query()
            .Include(me => me.ElectionGroup)
            .FirstOrDefaultAsync(me => me.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task<MajorityElectionCandidate> GetMajorityElectionCandidate(Guid id)
    {
        return await _candidateRepo.Query()
            .Include(c => c.MajorityElection)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task UpdateCandidateReferences(MajorityElectionCandidate candidate)
    {
        var candidateReferences = await _secondaryMajorityCandidateRepo.Query()
            .Where(c => c.CandidateReferenceId == candidate.Id)
            .ToListAsync();

        foreach (var candidateReference in candidateReferences)
        {
            // cannot use the mapper here, since that would overwrite some fields that should be untouched (id, position, incumbent, reporting type)
            candidateReference.FirstName = candidate.FirstName;
            candidateReference.LastName = candidate.LastName;
            candidateReference.PoliticalFirstName = candidate.PoliticalFirstName;
            candidateReference.PoliticalLastName = candidate.PoliticalLastName;
            candidateReference.Occupation = candidate.Occupation;
            candidateReference.OccupationTitle = candidate.OccupationTitle;
            candidateReference.Locality = candidate.Locality;
            candidateReference.PartyShortDescription = candidate.PartyShortDescription;
            candidateReference.PartyLongDescription = candidate.PartyLongDescription;
            candidateReference.DateOfBirth = candidate.DateOfBirth;
            candidateReference.Sex = candidate.Sex;
            candidateReference.Title = candidate.Title;
            candidateReference.ZipCode = candidate.ZipCode;
            candidateReference.Origin = candidate.Origin;
            candidateReference.Country = candidate.Country;
            candidateReference.Street = candidate.Street;
            candidateReference.HouseNumber = candidate.HouseNumber;
        }

        await _secondaryMajorityCandidateRepo.UpdateRange(candidateReferences);
    }

    private void TruncateCandidateNumber(MajorityElectionCandidate candidate)
    {
        if (candidate.Number.Length <= 10)
        {
            return;
        }

        // old events can contain a number which is longer than 10 chars
        candidate.Number = candidate.Number[..10];
    }
}
