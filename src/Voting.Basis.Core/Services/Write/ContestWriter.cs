// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Shared.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Contest = Voting.Basis.Core.Domain.Contest;

namespace Voting.Basis.Core.Services.Write;

public class ContestWriter
{
    private readonly ILogger<ContestWriter> _logger;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly ContestReader _contestReader;
    private readonly PermissionService _permissionService;
    private readonly ContestValidationService _contestValidationService;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly ContestMerger _contestMerger;
    private readonly ContestDeleter _contestDeleter;
    private readonly IAuth _auth;

    public ContestWriter(
        ILogger<ContestWriter> logger,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ContestReader contestReader,
        PermissionService permissionService,
        ContestValidationService contestValidationService,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        ContestMerger contestMerger,
        ContestDeleter contestDeleter,
        IAuth auth)
    {
        _logger = logger;
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _contestReader = contestReader;
        _permissionService = permissionService;
        _contestValidationService = contestValidationService;
        _doiRepo = doiRepo;
        _contestMerger = contestMerger;
        _contestDeleter = contestDeleter;
        _auth = auth;
    }

    [SuppressMessage(
        "General",
        "RCS1079:Throwing of new NotImplementedException.",
        Justification = "This is just a fallback and only applies if a new availability type is introduced but not yet handled.")]
    public async Task Create(Contest data)
    {
        if (data.Date == default)
        {
            throw new ValidationException("Date cannot be undefined");
        }

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(data.DomainOfInfluenceId);
        await EnsureValidPreviousContest(data);
        await EnsureValidDomainOfInfluence(data);

        var availability = await _contestReader.CheckAvailabilityInternal(data.Date, data.DomainOfInfluenceId);
        var contestAggregate = _aggregateFactory.New<ContestAggregate>();

        switch (availability.Availability)
        {
            case ContestDateAvailability.AlreadyExists:
                throw new AlreadyExistsException("A contest already exists on this date");
            case ContestDateAvailability.ExistsOnChildTenant:
                await _contestMerger.MergeContests(contestAggregate, data, availability.Contests.Select(c => c.Id).ToList());
                return;
            case ContestDateAvailability.Available:
            case ContestDateAvailability.CloseToOtherContestDate:
            case ContestDateAvailability.SameAsPreConfiguredDate:
                contestAggregate.CreateFrom(data);
                await _aggregateRepository.Save(contestAggregate);
                return;
        }

        throw new NotImplementedException("Availability not configured");
    }

    public async Task Update(Contest data)
    {
        if (data.Date == default)
        {
            throw new ValidationException("Date cannot be null");
        }

        var contest = await _aggregateRepository.GetById<ContestAggregate>(data.Id);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(contest.DomainOfInfluenceId);
        await EnsureValidPreviousContest(data);

        if (contest.Date != data.Date.Date)
        {
            var availability = await _contestReader.CheckAvailabilityInternal(data.Date, contest.DomainOfInfluenceId);

            if (availability.Availability == ContestDateAvailability.AlreadyExists)
            {
                throw new AlreadyExistsException("A contest already exists on this date");
            }

            if (availability.Availability == ContestDateAvailability.ExistsOnChildTenant)
            {
                await _contestMerger.MergeContests(contest, data, availability.Contests.Select(c => c.Id).ToList());
                return;
            }
        }

        contest.UpdateFrom(data);
        await _aggregateRepository.Save(contest);
    }

    public async Task Delete(Guid contestId)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(contestId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(contest.DomainOfInfluenceId);
        await _contestValidationService.EnsureContestNotSetAsPreviousContest(contestId);

        await _contestDeleter.Delete(contest);
    }

    public async Task Archive(Guid contestId, DateTime? archivePer)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(contestId);

        await _permissionService.EnsureIsOwnerOfDomainOfInfluence(contest.DomainOfInfluenceId);

        contest.Archive(archivePer);
        await _aggregateRepository.Save(contest);
        _logger.LogInformation("Archived contest {ContestId}.", contestId);
    }

    public async Task PastUnlock(Guid contestId)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(contestId);

        await _permissionService.EnsureCanReadContest(contestId);

        contest.PastUnlock();
        await _aggregateRepository.Save(contest);
        _logger.LogInformation("Unlocked past contest {ContestId}.", contestId);
    }

    internal async Task<bool> TryEndTestingPhase(Guid id)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(id);
        if (!contest.TryEndTestingPhase())
        {
            return false;
        }

        await _aggregateRepository.Save(contest);
        _logger.LogInformation("Ended testing phase for contest {ContestId}.", id);
        return true;
    }

    internal async Task<bool> TrySetPastLocked(Guid id)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(id);
        if (!contest.TrySetPastLocked())
        {
            return false;
        }

        await _aggregateRepository.Save(contest);
        _logger.LogInformation("Set past locked for contest {ContestId}.", id);
        return true;
    }

    internal async Task<bool> TryArchive(Guid id)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(id);
        if (!contest.TryArchive())
        {
            return false;
        }

        await _aggregateRepository.Save(contest);
        _logger.LogInformation("Archived contest {ContestId}.", id);
        return true;
    }

    internal async Task StartContestImport(Guid id)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(id);
        contest.StartContestImport();
        await _aggregateRepository.Save(contest);
    }

    internal async Task StartPoliticalBusinessImport(Guid id)
    {
        var contest = await _aggregateRepository.GetById<ContestAggregate>(id);
        contest.StartPoliticalBusinessesImport();
        await _aggregateRepository.Save(contest);
    }

    private async Task EnsureValidPreviousContest(Contest contest)
    {
        if (!contest.PreviousContestId.HasValue)
        {
            return;
        }

        var doi = await _doiRepo.GetByKey(contest.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(nameof(DomainOfInfluence), contest.DomainOfInfluenceId);

        if (doi.CantonDefaults.InternalPlausibilisationDisabled)
        {
            throw new ValidationException("internal plausibilisation are disabled for this canton");
        }

        var pastContests = await _contestReader.ListPast(contest.Date, contest.DomainOfInfluenceId);

        if (pastContests.All(c => c.Id != contest.PreviousContestId))
        {
            throw new ValidationException("invalid previous contest id");
        }
    }

    private async Task EnsureValidDomainOfInfluence(Contest contest)
    {
        var doi = await _doiRepo.GetByKey(contest.DomainOfInfluenceId)
                  ?? throw new EntityNotFoundException(nameof(DomainOfInfluence), contest.DomainOfInfluenceId);

        if (!doi.CantonDefaults.CreateContestOnHighestHierarchicalLevelEnabled || doi.ParentId == null)
        {
            return;
        }

        var tenantId = _auth.Tenant.Id;
        var hasRootDomainOfInfluences = await _doiRepo.Query().AnyAsync(x => x.SecureConnectId == tenantId && x.ParentId == null);
        if (hasRootDomainOfInfluences)
        {
            throw new ValidationException("tenant has root domain of influences, so one must be selected");
        }
    }
}
