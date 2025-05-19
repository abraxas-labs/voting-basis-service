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
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Basis.Core.EventProcessors;

public class VoteProcessor :
    IEventProcessor<VoteCreated>,
    IEventProcessor<VoteUpdated>,
    IEventProcessor<VoteAfterTestingPhaseUpdated>,
    IEventProcessor<VoteActiveStateUpdated>,
    IEventProcessor<VoteDeleted>,
    IEventProcessor<VoteToNewContestMoved>,
    IEventProcessor<BallotCreated>,
    IEventProcessor<BallotUpdated>,
    IEventProcessor<BallotAfterTestingPhaseUpdated>,
    IEventProcessor<BallotDeleted>
{
    private readonly IDbRepository<DataContext, Vote> _repo;
    private readonly SimplePoliticalBusinessBuilder<Vote> _simplePoliticalBusinessBuilder;
    private readonly IDbRepository<DataContext, Ballot> _ballotRepo;
    private readonly BallotQuestionRepo _ballotQuestionRepo;
    private readonly TieBreakQuestionRepo _tieBreakQuestionRepo;
    private readonly IMapper _mapper;
    private readonly EventLoggerAdapter _eventLogger;
    private readonly ILogger<VoteProcessor> _logger;

    public VoteProcessor(
        ILogger<VoteProcessor> logger,
        IDbRepository<DataContext, Vote> repo,
        IDbRepository<DataContext, Ballot> ballotRepo,
        BallotQuestionRepo ballotQuestionRepo,
        TieBreakQuestionRepo tieBreakQuestionRepo,
        IMapper mapper,
        EventLoggerAdapter eventLogger,
        SimplePoliticalBusinessBuilder<Vote> simplePoliticalBusinessBuilder)
    {
        _logger = logger;
        _repo = repo;
        _ballotRepo = ballotRepo;
        _ballotQuestionRepo = ballotQuestionRepo;
        _mapper = mapper;
        _tieBreakQuestionRepo = tieBreakQuestionRepo;
        _eventLogger = eventLogger;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
    }

    public async Task Process(VoteCreated eventData)
    {
        var model = _mapper.Map<Vote>(eventData.Vote);

        PatchOldEventIfNecessary(model);

        await _repo.Create(model);
        await _simplePoliticalBusinessBuilder.Create(model);

        await _eventLogger.LogVoteEvent(eventData, model);
    }

    public async Task Process(VoteUpdated eventData)
    {
        var model = _mapper.Map<Vote>(eventData.Vote);

        PatchOldEventIfNecessary(model);
        await CalculateSubTypeForVoteWithoutFetchedBallots(model);

        await _repo.Update(model);
        await _simplePoliticalBusinessBuilder.Update(model);
        await _eventLogger.LogVoteEvent(eventData, model);
    }

    public async Task Process(VoteAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var vote = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, vote);
        await CalculateSubTypeForVoteWithoutFetchedBallots(vote);

        await _repo.Update(vote);
        await _simplePoliticalBusinessBuilder.Update(vote);
        await _eventLogger.LogVoteEvent(eventData, vote);
    }

    public async Task Process(VoteDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.VoteId);
        try
        {
            var vote = await GetVote(id);
            await _repo.DeleteByKey(id);
            await _simplePoliticalBusinessBuilder.Delete(vote);
            await _eventLogger.LogVoteEvent(eventData, vote);
        }
        catch (EntityNotFoundException)
        {
            // skip event processing to prevent race condition if vote was deleted from other process.
            _logger.LogWarning("event 'VoteDeleted' skipped. vote {id} has already been deleted", id);
        }
    }

    public async Task Process(VoteToNewContestMoved eventData)
    {
        var voteId = GuidParser.Parse(eventData.VoteId);
        var existingModel = await GetVote(voteId);

        existingModel.ContestId = GuidParser.Parse(eventData.NewContestId);

        await CalculateSubTypeForVoteWithoutFetchedBallots(existingModel);

        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogVoteEvent(eventData, existingModel);
    }

    public async Task Process(BallotCreated eventData)
    {
        var model = _mapper.Map<Ballot>(eventData.Ballot);

        SetDefaultValues(model);

        await _ballotRepo.Create(model);
        await _eventLogger.LogBallotEvent(eventData, await GetBallot(model.Id));
        await UpdateVoteSubTypeIfNecessary(model.VoteId);
    }

    public async Task Process(BallotUpdated eventData)
    {
        var model = _mapper.Map<Ballot>(eventData.Ballot);

        SetDefaultValues(model);

        var existingModel = await GetBallot(model.Id);

        await _ballotQuestionRepo.Replace(model.Id, model.BallotQuestions);
        await _tieBreakQuestionRepo.Replace(model.Id, model.TieBreakQuestions);
        await _ballotRepo.Update(model);

        await _eventLogger.LogBallotEvent(eventData, model, existingModel.Vote.ContestId, existingModel.Vote.DomainOfInfluenceId);
        await UpdateVoteSubTypeIfNecessary(existingModel.VoteId);
    }

    public async Task Process(BallotAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var ballot = await GetBallot(id);
        _mapper.Map(eventData, ballot);

        SetDefaultValues(ballot);

        await _ballotQuestionRepo.Replace(ballot.Id, ballot.BallotQuestions);
        await _tieBreakQuestionRepo.Replace(ballot.Id, ballot.TieBreakQuestions);
        await _ballotRepo.Update(ballot);

        await _eventLogger.LogBallotEvent(eventData, ballot);
        await UpdateVoteSubTypeIfNecessary(ballot.VoteId);
    }

    public async Task Process(BallotDeleted eventData)
    {
        var ballotId = GuidParser.Parse(eventData.BallotId);
        try
        {
            var ballot = await GetBallot(ballotId);
            await _ballotRepo.DeleteByKey(ballotId);
            await _eventLogger.LogBallotEvent(eventData, ballot);
            await UpdateVoteSubTypeIfNecessary(ballot.VoteId);
        }
        catch (EntityNotFoundException)
        {
            // skip event processing to prevent race condition if ballot was deleted from other process.
            _logger.LogWarning("event 'BallotDeleted' skipped. ballot {id} has already been deleted", ballotId);
        }
    }

    public async Task Process(VoteActiveStateUpdated eventData)
    {
        var voteId = GuidParser.Parse(eventData.VoteId);
        var existingModel = await GetVote(voteId);

        existingModel.Active = eventData.Active;
        await CalculateSubTypeForVoteWithoutFetchedBallots(existingModel);
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel);
        await _eventLogger.LogVoteEvent(eventData, existingModel);
    }

    private void PatchOldEventIfNecessary(Vote vote)
    {
        if (vote.ReviewProcedure == VoteReviewProcedure.Unspecified)
        {
            vote.ReviewProcedure = VoteReviewProcedure.Electronically;
        }

        if (vote.Type == VoteType.Unspecified)
        {
            vote.Type = VoteType.QuestionsOnSingleBallot;
        }
    }

    private async Task CalculateSubTypeForVoteWithoutFetchedBallots(Vote vote)
    {
        var hasBallotWithVariantBallotType = await _ballotRepo.Query()
            .AnyAsync(x => x.VoteId == vote.Id && x.BallotType == BallotType.VariantsBallot);
        vote.UpdateSubTypeManually(hasBallotWithVariantBallotType);
    }

    private async Task<Vote> GetVote(Guid id)
    {
        return await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task<Ballot> GetBallot(Guid id)
    {
        return await _ballotRepo.Query()
            .Include(b => b.Vote)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task UpdateVoteSubTypeIfNecessary(Guid voteId)
    {
        var voteInfo = await _repo.Query()
            .Where(x => x.Id == voteId)
            .Select(x => new
            {
                Vote = x,
                HasBallotWithVariantBallotType = x.Ballots.Any(b => b.BallotType == BallotType.VariantsBallot),
            })
            .FirstAsync();
        voteInfo.Vote.UpdateSubTypeManually(voteInfo.HasBallotWithVariantBallotType);
        await _simplePoliticalBusinessBuilder.UpdateSubTypeIfNecessary(voteInfo.Vote);
    }

    private void SetDefaultValues(Ballot ballot)
    {
        // Set default ballot question type value since the old eventData (before introducing the type) can contain the unspecified value.
        foreach (var ballotQuestion in ballot.BallotQuestions)
        {
            if (ballotQuestion.Type == BallotQuestionType.Unspecified)
            {
                ballotQuestion.Type = ballotQuestion.Number == 1
                    ? BallotQuestionType.MainBallot
                    : BallotQuestionType.CounterProposal;
            }
        }
    }
}
