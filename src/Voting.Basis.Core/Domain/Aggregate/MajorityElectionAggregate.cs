// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.Models;
using Voting.Basis.Core.Utils;
using Voting.Lib.Common;
using BallotNumberGeneration = Voting.Basis.Data.Models.BallotNumberGeneration;
using MajorityElectionCandidateReportingType = Voting.Basis.Data.Models.MajorityElectionCandidateReportingType;
using MajorityElectionMandateAlgorithm = Voting.Basis.Data.Models.MajorityElectionMandateAlgorithm;
using MajorityElectionResultEntry = Voting.Basis.Data.Models.MajorityElectionResultEntry;
using MajorityElectionReviewProcedure = Voting.Basis.Data.Models.MajorityElectionReviewProcedure;
using SexType = Abraxas.Voting.Basis.Shared.V1.SexType;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="MajorityElection"/>.
/// </summary>
public class MajorityElectionAggregate : BaseHasContestAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<MajorityElection> _validator;
    private readonly IValidator<IEnumerable<EntityOrder>> _entityOrdersValidator;
    private readonly IValidator<MajorityElectionCandidate> _candidateValidator;
    private readonly IValidator<MajorityElectionBallotGroup> _ballotGroupValidator;
    private readonly IValidator<MajorityElectionBallotGroupCandidates> _ballotGroupCandidatesValidator;

    public MajorityElectionAggregate(
        IMapper mapper,
        EventInfoProvider eventInfoProvider,
        IValidator<MajorityElection> validator,
        IValidator<IEnumerable<EntityOrder>> entityOrdersValidator,
        IValidator<MajorityElectionCandidate> candidateValidator,
        IValidator<MajorityElectionBallotGroup> ballotGroupValidator,
        IValidator<MajorityElectionBallotGroupCandidates> ballotGroupCandidatesValidator)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
        _entityOrdersValidator = entityOrdersValidator;
        _candidateValidator = candidateValidator;
        _ballotGroupValidator = ballotGroupValidator;
        _ballotGroupCandidatesValidator = ballotGroupCandidatesValidator;
        PoliticalBusinessNumber = string.Empty;
        OfficialDescription = new Dictionary<string, string>();
        ShortDescription = new Dictionary<string, string>();

        Candidates = new List<MajorityElectionCandidate>();
        BallotGroups = new List<MajorityElectionBallotGroup>();
        SecondaryMajorityElections = new List<SecondaryMajorityElection>();
    }

    public override string AggregateName => "voting-majorityElections";

    public string PoliticalBusinessNumber { get; private set; }

    public Dictionary<string, string> OfficialDescription { get; private set; }

    public Dictionary<string, string> ShortDescription { get; private set; }

    public int NumberOfMandates { get; private set; }

    public MajorityElectionMandateAlgorithm MandateAlgorithm { get; private set; }

    public bool CandidateCheckDigit { get; private set; }

    public int BallotBundleSize { get; private set; }

    public int BallotBundleSampleSize { get; private set; }

    public bool AutomaticBallotBundleNumberGeneration { get; private set; }

    public bool AutomaticBallotNumberGeneration { get; private set; }

    public BallotNumberGeneration BallotNumberGeneration { get; private set; }

    public bool AutomaticEmptyVoteCounting { get; private set; }

    public bool EnforceEmptyVoteCountingForCountingCircles { get; private set; }

    public MajorityElectionResultEntry ResultEntry { get; private set; }

    public bool EnforceResultEntryForCountingCircles { get; private set; }

    public Guid DomainOfInfluenceId { get; private set; }

    public bool Active { get; private set; }

    public ElectionGroup? ElectionGroup { get; set; }

    public List<MajorityElectionCandidate> Candidates { get; private set; }

    public List<MajorityElectionBallotGroup> BallotGroups { get; private set; }

    public List<SecondaryMajorityElection> SecondaryMajorityElections { get; private set; }

    public MajorityElectionReviewProcedure ReviewProcedure { get; private set; }

    public bool EnforceReviewProcedureForCountingCircles { get; private set; }

    public bool EnforceCandidateCheckDigitForCountingCircles { get; private set; }

    public bool IndividualCandidatesDisabled { get; private set; }

    public int? FederalIdentification { get; private set; }

    public List<SecondaryMajorityElection> SecondaryMajorityElectionsOnSameBallot => SecondaryMajorityElections.Where(sme => !sme.IsOnSeparateBallot).ToList();

    public bool? EVotingApproved { get; private set; }

    public void CreateFrom(MajorityElection majorityElection)
    {
        if (majorityElection.Id == default)
        {
            majorityElection.Id = Guid.NewGuid();
        }

        _validator.ValidateAndThrow(majorityElection);

        var ev = new MajorityElectionCreated
        {
            MajorityElection = _mapper.Map<MajorityElectionEventData>(majorityElection),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(majorityElection.ContestId));
    }

    public void UpdateFrom(MajorityElection majorityElection)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _validator.ValidateAndThrow(majorityElection);

        // We only set a different e-voting approved on create or approval update.
        majorityElection.EVotingApproved = EVotingApproved;

        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, majorityElection.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(MandateAlgorithm, majorityElection.MandateAlgorithm);

        if (majorityElection.NumberOfMandates != NumberOfMandates && Active)
        {
            throw new MajorityElectionActiveNumberOfMandatesChangeException(Id);
        }

        var ev = new MajorityElectionUpdated
        {
            MajorityElection = _mapper.Map<MajorityElectionEventData>(majorityElection),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateAfterTestingPhaseEnded(MajorityElection majorityElection)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _validator.ValidateAndThrow(majorityElection);

        // Active shouldn't be changed by updates after the testing phase, but also shouldn't throw an error,
        // since sometimes the wrong values is provided, which would result in a "modified exception"
        majorityElection.Active = Active;

        ValidationUtils.EnsureNotModified(NumberOfMandates, majorityElection.NumberOfMandates);
        ValidationUtils.EnsureNotModified(MandateAlgorithm, majorityElection.MandateAlgorithm);
        ValidationUtils.EnsureNotModified(CandidateCheckDigit, majorityElection.CandidateCheckDigit);
        ValidationUtils.EnsureNotModified(BallotBundleSize, majorityElection.BallotBundleSize);
        ValidationUtils.EnsureNotModified(AutomaticBallotBundleNumberGeneration, majorityElection.AutomaticBallotBundleNumberGeneration);
        ValidationUtils.EnsureNotModified(AutomaticBallotNumberGeneration, majorityElection.AutomaticBallotNumberGeneration);
        ValidationUtils.EnsureNotModified(BallotNumberGeneration, majorityElection.BallotNumberGeneration);
        ValidationUtils.EnsureNotModified(AutomaticEmptyVoteCounting, majorityElection.AutomaticEmptyVoteCounting);
        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, majorityElection.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(ContestId, majorityElection.ContestId);
        ValidationUtils.EnsureNotModified(BallotBundleSampleSize, majorityElection.BallotBundleSampleSize);
        ValidationUtils.EnsureNotModified(ResultEntry, majorityElection.ResultEntry);
        ValidationUtils.EnsureNotModified(ReviewProcedure, majorityElection.ReviewProcedure);
        ValidationUtils.EnsureNotModified(EnforceReviewProcedureForCountingCircles, majorityElection.EnforceReviewProcedureForCountingCircles);
        ValidationUtils.EnsureNotModified(EnforceCandidateCheckDigitForCountingCircles, majorityElection.EnforceCandidateCheckDigitForCountingCircles);
        ValidationUtils.EnsureNotModified(IndividualCandidatesDisabled, majorityElection.IndividualCandidatesDisabled);
        ValidationUtils.EnsureNotModified(FederalIdentification, majorityElection.FederalIdentification);

        var ev = new MajorityElectionAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = majorityElection.Id.ToString(),
            PoliticalBusinessNumber = majorityElection.PoliticalBusinessNumber,
            OfficialDescription = { majorityElection.OfficialDescription },
            ShortDescription = { majorityElection.ShortDescription },
            EnforceEmptyVoteCountingForCountingCircles = majorityElection.EnforceEmptyVoteCountingForCountingCircles,
            EnforceResultEntryForCountingCircles = majorityElection.EnforceResultEntryForCountingCircles,
            ReportDomainOfInfluenceLevel = majorityElection.ReportDomainOfInfluenceLevel,
        };

        RaiseEvent(ev);
    }

    public void UpdateActiveState(bool active)
    {
        EnsureCanSetActive(active);
        EnsureEVotingNotApproved();

        var ev = new MajorityElectionActiveStateUpdated
        {
            MajorityElectionId = Id.ToString(),
            Active = active,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateEVotingApproval(bool approved)
    {
        EnsureCanSetActive(approved);

        if (EVotingApproved == null)
        {
            throw new ValidationException($"Majority election {Id} does not support E-Voting");
        }

        var ev = new MajorityElectionEVotingApprovalUpdated
        {
            MajorityElectionId = Id.ToString(),
            Approved = approved,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public bool TryApproveEVoting()
    {
        if (EVotingApproved == true)
        {
            return false;
        }

        try
        {
            UpdateEVotingApproval(true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Delete(bool ignoreChecks = false)
    {
        EnsureNotDeleted();

        if (!ignoreChecks)
        {
            EnsureEVotingNotApproved();
        }

        if (SecondaryMajorityElections.Count > 0)
        {
            throw new MajorityElectionWithExistingSecondaryElectionsException();
        }

        var ev = new MajorityElectionDeleted
        {
            MajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateCandidateFrom(MajorityElectionCandidate candidate, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        if (candidate.Id == default)
        {
            candidate.Id = Guid.NewGuid();
        }

        _candidateValidator.ValidateAndThrow(candidate);

        if (candidate.Position != Candidates.Count + 1)
        {
            throw new ValidationException("Candidate position should be continuous");
        }

        EnsureCandidateIsValid(candidate, null, candidateValidationParams, IndividualCandidatesDisabled);
        EnsureUniqueCandidatePosition(candidate);
        EnsureUniqueCandidateNumber(candidate);

        candidate.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(candidate.Number);

        var ev = new MajorityElectionCandidateCreated
        {
            MajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(candidate),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateFrom(MajorityElectionCandidate candidate, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _candidateValidator.ValidateAndThrow(candidate);

        var existingCandidate = FindCandidate(candidate.Id);
        if (candidate.Position != existingCandidate.Position)
        {
            throw new ValidationException("Cannot change the candidate position via an update");
        }

        EnsureCandidateIsValid(candidate, existingCandidate, candidateValidationParams, IndividualCandidatesDisabled);
        EnsureUniqueCandidatePosition(candidate);
        EnsureUniqueCandidateNumber(candidate);

        candidate.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(candidate.Number);

        var ev = new MajorityElectionCandidateUpdated
        {
            MajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(candidate),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateAfterTestingPhaseEnded(MajorityElectionCandidate candidate, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _candidateValidator.ValidateAndThrow(candidate);

        candidate.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(candidate.Number);

        var existingCandidate = FindCandidate(candidate.Id)
                                ?? throw new ValidationException($"Candidate {candidate.Id} does not exist");

        ValidationUtils.EnsureNotModified(existingCandidate.Number, candidate.Number);
        ValidationUtils.EnsureNotModified(existingCandidate.CheckDigit, candidate.CheckDigit);
        ValidationUtils.EnsureNotModified(existingCandidate.Position, candidate.Position);
        EnsureCandidateIsValid(candidate, existingCandidate, candidateValidationParams, IndividualCandidatesDisabled);

        var ev = new MajorityElectionCandidateAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = candidate.Id.ToString(),
            MajorityElectionId = Id.ToString(),
            FirstName = candidate.FirstName,
            LastName = candidate.LastName,
            PoliticalFirstName = candidate.PoliticalFirstName,
            PoliticalLastName = candidate.PoliticalLastName,
            DateOfBirth = candidate.DateOfBirth?.ToTimestamp(),
            Sex = _mapper.Map<SexType>(candidate.Sex),
            Occupation = { candidate.Occupation },
            Title = candidate.Title,
            OccupationTitle = { candidate.OccupationTitle },
            Incumbent = candidate.Incumbent,
            ZipCode = candidate.ZipCode,
            Locality = candidate.Locality,
            Party = { candidate.PartyShortDescription },
            PartyLongDescription = { candidate.PartyLongDescription },
            Origin = candidate.Origin,
            Country = candidate.Country,
            Street = candidate.Street,
            HouseNumber = candidate.HouseNumber,
            ReportingType = _mapper.Map<SharedProto.MajorityElectionCandidateReportingType>(candidate.ReportingType),
        };

        RaiseEvent(ev);
    }

    public void ReorderCandidates(IReadOnlyCollection<EntityOrder> orders)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _entityOrdersValidator.ValidateAndThrow(orders);

        var ids = orders.Select(o => o.Id).ToHashSet();
        if (ids.Count != orders.Count)
        {
            throw new ValidationException("Duplicate candidate ids while reordering");
        }

        if (ids.Count != Candidates.Count || Candidates.Any(l => !ids.Contains(l.Id)))
        {
            throw new ValidationException("Not all candidate ids provided while reordering");
        }

        var ev = new MajorityElectionCandidatesReordered
        {
            MajorityElectionId = Id.ToString(),
            CandidateOrders = _mapper.Map<EntityOrdersEventData>(orders),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteCandidate(Guid candidateId)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureCandidateExists(candidateId);
        EnsureIsNotInBallotGroup(candidateId);

        foreach (var secondaryMajorityElection in SecondaryMajorityElections)
        {
            var candidateReference = secondaryMajorityElection.CandidateReferences.Find(cr => cr.CandidateId == candidateId);
            if (candidateReference == null)
            {
                continue;
            }

            var deleteReferenceEvent = new SecondaryMajorityElectionCandidateReferenceDeleted
            {
                SecondaryMajorityElectionCandidateReferenceId = candidateReference.Id.ToString(),
                SecondaryMajorityElectionId = secondaryMajorityElection.Id.ToString(),
                PrimaryMajorityElectionId = Id.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
            };

            RaiseEvent(deleteReferenceEvent);
        }

        var ev = new MajorityElectionCandidateDeleted
        {
            MajorityElectionCandidateId = candidateId.ToString(),
            MajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateSecondaryMajorityElectionFrom(SecondaryMajorityElection data)
    {
        EnsureNotDeleted();

        if (AnyElectionIsActive() && BallotGroups.Count > 0)
        {
            throw new SecondaryMajorityElectionCreateWithActiveElectionsAndBallotGroupsException(Id);
        }

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
        }

        if (EVotingApproved.HasValue)
        {
            data.EVotingApproved = false;
        }

        var ev = new SecondaryMajorityElectionCreated
        {
            SecondaryMajorityElection = _mapper.Map<SecondaryMajorityElectionEventData>(data),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionFrom(SecondaryMajorityElection data)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(data.Id);
        var sme = GetSecondaryMajorityElection(data.Id);

        if (data.NumberOfMandates != sme.NumberOfMandates && sme.Active)
        {
            throw new MajorityElectionActiveNumberOfMandatesChangeException(data.Id);
        }

        data.IsOnSeparateBallot = sme.IsOnSeparateBallot; // immutable

        // We only set a different e-voting approved on create or approval update.
        data.EVotingApproved = sme.EVotingApproved;

        var ev = new SecondaryMajorityElectionUpdated
        {
            SecondaryMajorityElection = _mapper.Map<SecondaryMajorityElectionEventData>(data),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionAfterTestingPhaseEnded(SecondaryMajorityElection data)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(data.Id);
        var sme = GetSecondaryMajorityElection(data.Id);

        // Active shouldn't be changed by updates after the testing phase, but also shouldn't throw an error,
        // since sometimes the wrong values is provided, which would result in a "modified exception"
        data.Active = sme.Active;

        ValidationUtils.EnsureNotModified(sme.NumberOfMandates, data.NumberOfMandates);
        ValidationUtils.EnsureNotModified(sme.Active, data.Active);

        var ev = new SecondaryMajorityElectionAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = data.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            OfficialDescription = { data.OfficialDescription },
            ShortDescription = { data.ShortDescription },
            PoliticalBusinessNumber = data.PoliticalBusinessNumber,
        };

        RaiseEvent(ev);
    }

    public void DeleteSecondaryMajorityElection(Guid id, bool ignoreChecks)
    {
        EnsureNotDeleted();

        if (!ignoreChecks)
        {
            EnsureEVotingNotApproved(id);
        }

        var sme = GetSecondaryMajorityElection(id);

        var ev = new SecondaryMajorityElectionDeleted
        {
            SecondaryMajorityElectionId = id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionActiveState(Guid id, bool active)
    {
        EnsureCanSetActive(active);
        EnsureEVotingNotApproved(id);

        var sme = GetSecondaryMajorityElection(id);

        var ev = new SecondaryMajorityElectionActiveStateUpdated
        {
            SecondaryMajorityElectionId = id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            Active = active,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionEVotingApproval(Guid id, bool approved)
    {
        EnsureCanSetActive(approved);

        var sme = GetSecondaryMajorityElection(id);

        if (sme.EVotingApproved == null)
        {
            throw new ValidationException($"Secondary majority election {Id} does not support E-Voting");
        }

        var ev = new SecondaryMajorityElectionEVotingApprovalUpdated
        {
            SecondaryMajorityElectionId = id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            Approved = approved,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public bool TryApproveSecondaryMajorityElectionEVoting(Guid id)
    {
        var sme = GetSecondaryMajorityElection(id);
        if (sme.EVotingApproved == true)
        {
            return false;
        }

        try
        {
            UpdateSecondaryMajorityElectionEVotingApproval(id, true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void CreateSecondaryMajorityElectionCandidateFrom(MajorityElectionCandidate data, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(data.MajorityElectionId);

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
        }

        var sme = GetSecondaryMajorityElection(data.MajorityElectionId);
        EnsureCandidateIsValid(data, null, candidateValidationParams, sme.IndividualCandidatesDisabled);

        _candidateValidator.ValidateAndThrow(data);

        sme.EnsureValidCandidatePosition(data, true);
        sme.EnsureUniqueCandidateNumber(data, sme.CandidateReferences);

        data.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(data.Number);

        var ev = new SecondaryMajorityElectionCandidateCreated
        {
            SecondaryMajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(data),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionCandidateFrom(MajorityElectionCandidate data, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(data.MajorityElectionId);
        _candidateValidator.ValidateAndThrow(data);

        var sme = GetSecondaryMajorityElection(data.MajorityElectionId);
        var existingSmeCandidate = sme.GetCandidate(data.Id);

        EnsureCandidateIsValid(data, existingSmeCandidate, candidateValidationParams, sme.IndividualCandidatesDisabled);
        sme.EnsureValidCandidatePosition(data, false);
        sme.EnsureUniqueCandidateNumber(data, sme.CandidateReferences);

        data.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(data.Number);

        var ev = new SecondaryMajorityElectionCandidateUpdated
        {
            SecondaryMajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(data),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            PrimaryMajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionCandidateAfterTestingPhaseEnded(MajorityElectionCandidate data, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(data.MajorityElectionId);
        _candidateValidator.ValidateAndThrow(data);

        data.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(data.Number);

        var sme = GetSecondaryMajorityElection(data.MajorityElectionId);
        var existingCandidate = sme.GetCandidate(data.Id);

        ValidationUtils.EnsureNotModified(existingCandidate.Number, data.Number);
        ValidationUtils.EnsureNotModified(existingCandidate.CheckDigit, data.CheckDigit);
        ValidationUtils.EnsureNotModified(existingCandidate.Position, data.Position);
        EnsureCandidateIsValid(data, existingCandidate, candidateValidationParams, sme.IndividualCandidatesDisabled);

        var ev = new SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = data.Id.ToString(),
            SecondaryMajorityElectionId = sme.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            FirstName = data.FirstName,
            LastName = data.LastName,
            PoliticalFirstName = data.PoliticalFirstName,
            PoliticalLastName = data.PoliticalLastName,
            DateOfBirth = data.DateOfBirth?.ToTimestamp(),
            Sex = _mapper.Map<SexType>(data.Sex),
            Occupation = { data.Occupation },
            Title = data.Title,
            OccupationTitle = { data.OccupationTitle },
            Incumbent = data.Incumbent,
            ZipCode = data.ZipCode,
            Locality = data.Locality,
            Party = { data.PartyShortDescription },
            PartyLongDescription = { data.PartyLongDescription },
            Origin = data.Origin,
            Country = data.Country,
            Street = data.Street,
            HouseNumber = data.HouseNumber,
            ReportingType = _mapper.Map<SharedProto.MajorityElectionCandidateReportingType>(data.ReportingType),
        };

        RaiseEvent(ev);
    }

    public void DeleteSecondaryMajorityElectionCandidate(Guid secondaryMajorityElectionId, Guid candidateId)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(secondaryMajorityElectionId);

        var sme = GetSecondaryMajorityElection(secondaryMajorityElectionId);

        // ensure candidate exists
        sme.GetCandidate(candidateId);

        EnsureIsNotInBallotGroup(candidateId);

        var ev = new SecondaryMajorityElectionCandidateDeleted
        {
            SecondaryMajorityElectionCandidateId = candidateId.ToString(),
            SecondaryMajorityElectionId = sme.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateCandidateReferenceFrom(MajorityElectionCandidateReference data, bool testingPhaseEnded)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(data.SecondaryMajorityElectionId);

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
        }

        var sme = GetSecondaryMajorityElection(data.SecondaryMajorityElectionId);
        if (sme.CandidateReferences.Any(cr => cr.CandidateId == data.CandidateId))
        {
            throw new ValidationException("Candidate reference already exists");
        }

        sme.EnsureValidCandidatePosition(data, true);
        sme.EnsureUniqueCandidateNumber(data, sme.CandidateReferences, data.Number);

        var candidateReferenceEventData = _mapper.Map<MajorityElectionCandidateReferenceEventData>(data);
        candidateReferenceEventData.PrimaryMajorityElectionId = Id.ToString();
        candidateReferenceEventData.IsOnSeparateBallot = sme.IsOnSeparateBallot;
        candidateReferenceEventData.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(data.Number);
        EnsureValidCandidateReportingType(data, null, testingPhaseEnded, sme.IndividualCandidatesDisabled, c => c?.ReportingType);

        var ev = new SecondaryMajorityElectionCandidateReferenceCreated
        {
            MajorityElectionCandidateReference = candidateReferenceEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateReferenceFrom(MajorityElectionCandidateReference data, bool testingPhaseEnded)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(data.SecondaryMajorityElectionId);
        var sme = GetSecondaryMajorityElection(data.SecondaryMajorityElectionId);
        var existingReference = sme.GetCandidateReference(data.Id);

        if (data.CandidateId != existingReference.CandidateId)
        {
            throw new ValidationException($"{nameof(data.CandidateId)} is immutable");
        }

        if (testingPhaseEnded)
        {
            ValidationUtils.EnsureNotModified(existingReference.Number, data.Number);
            ValidationUtils.EnsureNotModified(existingReference.Position, data.Position);
        }

        sme.EnsureValidCandidatePosition(data, false);
        sme.EnsureUniqueCandidateNumber(data, sme.CandidateReferences, data.Number);

        var candidateReferenceEventData = _mapper.Map<MajorityElectionCandidateReferenceEventData>(data);
        candidateReferenceEventData.PrimaryMajorityElectionId = Id.ToString();
        candidateReferenceEventData.IsOnSeparateBallot = sme.IsOnSeparateBallot;
        candidateReferenceEventData.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(data.Number);
        EnsureValidCandidateReportingType(data, existingReference, testingPhaseEnded, sme.IndividualCandidatesDisabled, c => c?.ReportingType);

        var ev = new SecondaryMajorityElectionCandidateReferenceUpdated
        {
            MajorityElectionCandidateReference = candidateReferenceEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteCandidateReference(Guid secondaryMajorityElectionId, Guid candidateId)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(secondaryMajorityElectionId);
        EnsureIsNotInBallotGroup(candidateId);

        var sme = GetSecondaryMajorityElection(secondaryMajorityElectionId);
        sme.GetCandidateReference(candidateId);

        var ev = new SecondaryMajorityElectionCandidateReferenceDeleted
        {
            SecondaryMajorityElectionCandidateReferenceId = candidateId.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            SecondaryMajorityElectionId = sme.Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void ReorderSecondaryMajorityElectionCandidates(Guid secondaryMajorityElectionId, IReadOnlyCollection<EntityOrder> orders)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved(secondaryMajorityElectionId);
        _entityOrdersValidator.ValidateAndThrow(orders);

        var ids = orders.Select(o => o.Id).ToHashSet();
        if (ids.Count != orders.Count)
        {
            throw new ValidationException("Duplicate candidate ids while reordering");
        }

        var sme = GetSecondaryMajorityElection(secondaryMajorityElectionId);
        var totalSmeCandidates = sme.CandidateReferences.Count + sme.Candidates.Count;
        if (ids.Count != totalSmeCandidates || sme.Candidates.Any(l => !ids.Contains(l.Id)) || sme.CandidateReferences.Any(l => !ids.Contains(l.Id)))
        {
            throw new ValidationException("Not all candidate ids provided while reordering");
        }

        var ev = new SecondaryMajorityElectionCandidatesReordered
        {
            SecondaryMajorityElectionId = secondaryMajorityElectionId.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            IsOnSeparateBallot = sme.IsOnSeparateBallot,
            CandidateOrders = _mapper.Map<EntityOrdersEventData>(orders),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateBallotGroupFrom(MajorityElectionBallotGroup data)
    {
        EnsureNotDeleted();

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
        }

        foreach (var entry in data.Entries)
        {
            if (entry.Id == default)
            {
                entry.Id = Guid.NewGuid();
            }
        }

        _ballotGroupValidator.ValidateAndThrow(data);
        EnsureValidCreateUpdateBallotGroupEntries(data.Entries);

        if (BallotGroups.Any(bg => bg.Id != data.Id && bg.Position == data.Position))
        {
            throw new ValidationException($"Ballot group position {data.Position} is already taken.");
        }

        if (data.Position > BallotGroups.Count + 1)
        {
            throw new ValidationException($"The ballot group position {data.Position} is invalid, is non-continuous.");
        }

        var evBallotGroup = _mapper.Map<MajorityElectionBallotGroupEventData>(data);
        evBallotGroup.BlankRowCountUnused = true;

        var ev = new MajorityElectionBallotGroupCreated
        {
            BallotGroup = evBallotGroup,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateBallotGroup(MajorityElectionBallotGroup data)
    {
        EnsureNotDeleted();

        // new entries may have been added
        foreach (var entry in data.Entries)
        {
            if (entry.Id == default)
            {
                entry.Id = Guid.NewGuid();
            }
        }

        _ballotGroupValidator.ValidateAndThrow(data);
        EnsureValidCreateUpdateBallotGroupEntries(data.Entries);
        var ballotGroup = GetBallotGroup(data.Id);

        if (data.Position != ballotGroup.Position)
        {
            throw new ValidationException("Cannot change the ballot group position via an update");
        }

        if (ballotGroup.Entries.Any(e => data.Entries.Any(newE => newE.ElectionId == e.ElectionId && newE.Id != e.Id)))
        {
            throw new ValidationException("Cannot change the id of a ballot group entry");
        }

        var evBallotGroup = _mapper.Map<MajorityElectionBallotGroupEventData>(data);
        evBallotGroup.BlankRowCountUnused = true;

        var ev = new MajorityElectionBallotGroupUpdated
        {
            BallotGroup = evBallotGroup,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteBallotGroup(Guid id)
    {
        EnsureNotDeleted();
        GetBallotGroup(id);

        var ev = new MajorityElectionBallotGroupDeleted
        {
            BallotGroupId = id.ToString(),
            MajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateBallotGroupCandidates(MajorityElectionBallotGroupCandidates data, bool testingPhaseEnded)
    {
        EnsureNotDeleted();
        _ballotGroupCandidatesValidator.ValidateAndThrow(data);
        var ballotGroup = GetBallotGroup(data.BallotGroupId);

        if (data.EntryCandidates.Any(e => ballotGroup.Entries.All(bge => bge.Id != e.BallotGroupEntryId)))
        {
            throw new ValidationException("Some ballot group entries don't exist");
        }

        if (testingPhaseEnded)
        {
            // If the contest testing phase is over, only ballot groups with invalid configurations (candidate count is not ok) can be modified
            EnsureBallotGroupBlankRowsAreNotOk(ballotGroup);
        }

        EnsureBallotGroupCandidateEntriesAreOk(data);
        EnsureSelectedCandidatesAreSelectedInPrimaryElection(data);

        var ballotGroupCandidatesEventData = _mapper.Map<MajorityElectionBallotGroupCandidatesEventData>(data);
        ballotGroupCandidatesEventData.MajorityElectionId = Id.ToString();

        var ev = new MajorityElectionBallotGroupCandidatesUpdated
        {
            BallotGroupCandidates = ballotGroupCandidatesEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateElectionGroupFrom(ElectionGroup data)
    {
        EnsureNotDeleted();

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
        }

        if (ElectionGroup != null)
        {
            throw new ValidationException("Election group already exists");
        }

        if (data.Number <= 0)
        {
            throw new ValidationException("Election group number has to be greater than 0");
        }

        var ev = new ElectionGroupCreated
        {
            ElectionGroup = _mapper.Map<ElectionGroupEventData>(data),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteElectionGroup()
    {
        EnsureNotDeleted();

        if (ElectionGroup == null)
        {
            throw new ValidationException("Election group does not exist");
        }

        if (SecondaryMajorityElections.Count != 0)
        {
            throw new ValidationException("Can't delete election group, secondary majority elections exist");
        }

        var ev = new ElectionGroupDeleted
        {
            ElectionGroupId = ElectionGroup.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public override void MoveToNewContest(Guid newContestId)
    {
        EnsureNotDeleted();
        EnsureDifferentContest(newContestId);

        var ev = new MajorityElectionToNewContestMoved
        {
            MajorityElectionId = Id.ToString(),
            NewContestId = newContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(newContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case MajorityElectionCreated e:
                Apply(e);
                break;
            case MajorityElectionUpdated e:
                Apply(e);
                break;
            case MajorityElectionAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case MajorityElectionActiveStateUpdated e:
                Apply(e);
                break;
            case MajorityElectionEVotingApprovalUpdated e:
                Apply(e);
                break;
            case MajorityElectionDeleted _:
                Deleted = true;
                break;
            case MajorityElectionToNewContestMoved e:
                ContestId = GuidParser.Parse(e.NewContestId);
                break;
            case MajorityElectionCandidateCreated e:
                Apply(e);
                break;
            case MajorityElectionCandidateUpdated e:
                Apply(e);
                break;
            case MajorityElectionCandidateAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case MajorityElectionCandidatesReordered e:
                Apply(e);
                break;
            case MajorityElectionCandidateDeleted e:
                Apply(e);
                break;
            case ElectionGroupCreated e:
                Apply(e);
                break;
            case ElectionGroupDeleted _:
                ElectionGroup = null;
                break;
            case MajorityElectionBallotGroupCreated e:
                Apply(e);
                break;
            case MajorityElectionBallotGroupUpdated e:
                Apply(e);
                break;
            case MajorityElectionBallotGroupDeleted e:
                Apply(e);
                break;
            case MajorityElectionBallotGroupCandidatesUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCreated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionDeleted e:
                Apply(e);
                break;
            case SecondaryMajorityElectionActiveStateUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionEVotingApprovalUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidateCreated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidateUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidateDeleted e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidatesReordered e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidateReferenceCreated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidateReferenceUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCandidateReferenceDeleted e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(MajorityElectionCreated ev)
    {
        PatchOldEvents(ev.MajorityElection);
        _mapper.Map(ev.MajorityElection, this);
    }

    private void Apply(MajorityElectionUpdated ev)
    {
        PatchOldEvents(ev.MajorityElection);
        _mapper.Map(ev.MajorityElection, this);
    }

    private void PatchOldEvents(MajorityElectionEventData majorityElection)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (majorityElection.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.MajorityElectionReviewProcedure.Unspecified)
        {
            majorityElection.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.MajorityElectionReviewProcedure.Electronically;
        }

        if (majorityElection.AutomaticBallotNumberGeneration == null)
        {
            majorityElection.AutomaticBallotNumberGeneration = true;
        }
    }

    private void Apply(MajorityElectionAfterTestingPhaseUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(MajorityElectionActiveStateUpdated ev)
    {
        Active = ev.Active;
    }

    private void Apply(MajorityElectionEVotingApprovalUpdated ev)
    {
        EVotingApproved = ev.Approved;
    }

    private void Apply(MajorityElectionCandidateCreated ev)
    {
        var candidate = _mapper.Map<MajorityElectionCandidate>(ev.MajorityElectionCandidate);
        Candidates.Add(candidate);
    }

    private void Apply(MajorityElectionCandidateUpdated ev)
    {
        var candidate = FindCandidate(GuidParser.Parse(ev.MajorityElectionCandidate.Id));
        _mapper.Map(ev.MajorityElectionCandidate, candidate);
    }

    private void Apply(MajorityElectionCandidateAfterTestingPhaseUpdated ev)
    {
        var candidate = FindCandidate(GuidParser.Parse(ev.Id));
        _mapper.Map(ev, candidate);
    }

    private void Apply(MajorityElectionCandidatesReordered ev)
    {
        foreach (var order in ev.CandidateOrders.Orders)
        {
            var candidate = FindCandidate(GuidParser.Parse(order.Id));
            candidate.Position = order.Position;
        }
    }

    private void Apply(MajorityElectionCandidateDeleted ev)
    {
        var existingCandidate = FindCandidate(GuidParser.Parse(ev.MajorityElectionCandidateId));
        Candidates.Remove(existingCandidate);

        foreach (var candidate in Candidates.Where(c => c.Position > existingCandidate.Position))
        {
            candidate.Position--;
        }

        foreach (var sme in SecondaryMajorityElections)
        {
            sme.RemoveCandidateReferenceForCandidate(GuidParser.Parse(ev.MajorityElectionCandidateId));
        }
    }

    private void Apply(ElectionGroupCreated ev)
    {
        ElectionGroup = _mapper.Map<ElectionGroup>(ev.ElectionGroup);
    }

    private void Apply(MajorityElectionBallotGroupCreated ev)
    {
        var ballotGroup = _mapper.Map<MajorityElectionBallotGroup>(ev.BallotGroup);
        BallotGroups.Add(ballotGroup);
    }

    private void Apply(MajorityElectionBallotGroupUpdated ev)
    {
        var ballotGroup = GetBallotGroup(GuidParser.Parse(ev.BallotGroup.Id));
        var existingEntriessById = ballotGroup.Entries.ToDictionary(x => x.Id);

        // Overrides the ballot group and removes the existing entries.
        _mapper.Map(ev.BallotGroup, ballotGroup);

        foreach (var entry in ballotGroup.Entries)
        {
            if (existingEntriessById.TryGetValue(entry.Id, out var existingEntry))
            {
                entry.CandidateIds.Clear();
                entry.CandidateIds.AddRange(existingEntry.CandidateIds);
                entry.IndividualCandidatesVoteCount = existingEntry.IndividualCandidatesVoteCount;

                // When the blank row count is unused (new version) this field is set in the BallotGroupCandidatesUpdated event (already applied) instead of the BallotGroupUpdated event.
                if (ev.BallotGroup.BlankRowCountUnused)
                {
                    entry.BlankRowCount = existingEntry.BlankRowCount;
                }
            }
        }
    }

    private void Apply(MajorityElectionBallotGroupDeleted ev)
    {
        var toDelete = GetBallotGroup(GuidParser.Parse(ev.BallotGroupId));
        BallotGroups.Remove(toDelete);

        foreach (var ballotGroup in BallotGroups.Where(bg => bg.Position > toDelete.Position))
        {
            ballotGroup.Position--;
        }
    }

    private void Apply(MajorityElectionBallotGroupCandidatesUpdated ev)
    {
        var ballotGroup = GetBallotGroup(GuidParser.Parse(ev.BallotGroupCandidates.BallotGroupId));
        var eventEntries = ev.BallotGroupCandidates.EntryCandidates.ToDictionary(e => GuidParser.Parse(e.BallotGroupEntryId));

        foreach (var entry in ballotGroup.Entries)
        {
            if (eventEntries.TryGetValue(entry.Id, out var eventEntry))
            {
                entry.CandidateIds.Clear();
                entry.CandidateIds.AddRange(eventEntry.CandidateIds);
                entry.IndividualCandidatesVoteCount = eventEntry.IndividualCandidatesVoteCount;

                // Only set blank row count if it is not null (new version). In a previous event version this was set on create or update ballot group.
                if (eventEntry.BlankRowCount.HasValue)
                {
                    entry.BlankRowCount = eventEntry.BlankRowCount.Value;
                }
            }
        }
    }

    private void Apply(SecondaryMajorityElectionCreated ev)
    {
        var sme = _mapper.Map<SecondaryMajorityElection>(ev.SecondaryMajorityElection);
        SecondaryMajorityElections.Add(sme);
    }

    private void Apply(SecondaryMajorityElectionUpdated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElection.Id));
        _mapper.Map(ev.SecondaryMajorityElection, sme);
    }

    private void Apply(SecondaryMajorityElectionAfterTestingPhaseUpdated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.Id));
        _mapper.Map(ev, sme);
    }

    private void Apply(SecondaryMajorityElectionDeleted ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElectionId));
        SecondaryMajorityElections.Remove(sme);

        foreach (var ballotGroup in BallotGroups)
        {
            var smeBgEntry = ballotGroup.Entries.FirstOrDefault(e => e.ElectionId == sme.Id);
            if (smeBgEntry != null)
            {
                ballotGroup.Entries.Remove(smeBgEntry);
            }
        }
    }

    private void Apply(SecondaryMajorityElectionActiveStateUpdated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElectionId));
        sme.Active = ev.Active;
    }

    private void Apply(SecondaryMajorityElectionEVotingApprovalUpdated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElectionId));
        sme.EVotingApproved = ev.Approved;
    }

    private void Apply(SecondaryMajorityElectionCandidateCreated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElectionCandidate.MajorityElectionId));
        var candidate = _mapper.Map<MajorityElectionCandidate>(ev.SecondaryMajorityElectionCandidate);
        sme.Candidates.Add(candidate);
    }

    private void Apply(SecondaryMajorityElectionCandidateUpdated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElectionCandidate.MajorityElectionId));
        var candidate = sme.GetCandidate(GuidParser.Parse(ev.SecondaryMajorityElectionCandidate.Id));
        _mapper.Map(ev.SecondaryMajorityElectionCandidate, candidate);
    }

    private void Apply(SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated ev)
    {
        var id = GuidParser.Parse(ev.Id);
        var candidate = SecondaryMajorityElections.SelectMany(sme => sme.Candidates).Single(c => c.Id == id);
        _mapper.Map(ev, candidate);
    }

    private void Apply(SecondaryMajorityElectionCandidateDeleted ev)
    {
        var candidateId = GuidParser.Parse(ev.SecondaryMajorityElectionCandidateId);
        var sme = SecondaryMajorityElections.First(sme => sme.Candidates.Any(c => c.Id == candidateId));
        var candidate = sme.GetCandidate(candidateId);
        sme.Candidates.Remove(candidate);
        sme.ReorderCandidatesAfterDeletion(candidate.Position);
    }

    private void Apply(SecondaryMajorityElectionCandidatesReordered ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElectionId));

        foreach (var order in ev.CandidateOrders.Orders)
        {
            var orderId = GuidParser.Parse(order.Id);
            var candidate = sme.Candidates.Find(c => c.Id == orderId);
            if (candidate != null)
            {
                candidate.Position = order.Position;
            }
            else
            {
                var candidateReference = sme.CandidateReferences.Find(c => c.Id == orderId);
                if (candidateReference != null)
                {
                    candidateReference.Position = order.Position;
                }
            }
        }
    }

    private void Apply(SecondaryMajorityElectionCandidateReferenceCreated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.MajorityElectionCandidateReference.SecondaryMajorityElectionId));
        var reference = _mapper.Map<MajorityElectionCandidateReference>(ev.MajorityElectionCandidateReference);
        sme.CandidateReferences.Add(reference);
    }

    private void Apply(SecondaryMajorityElectionCandidateReferenceUpdated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.MajorityElectionCandidateReference.SecondaryMajorityElectionId));
        var reference = sme.GetCandidateReference(GuidParser.Parse(ev.MajorityElectionCandidateReference.Id));
        _mapper.Map(ev.MajorityElectionCandidateReference, reference);
    }

    private void Apply(SecondaryMajorityElectionCandidateReferenceDeleted ev)
    {
        var referenceId = GuidParser.Parse(ev.SecondaryMajorityElectionCandidateReferenceId);
        var sme = SecondaryMajorityElections.First(sme => sme.CandidateReferences.Any(c => c.Id == referenceId));
        var reference = sme.GetCandidateReference(referenceId);
        sme.CandidateReferences.Remove(reference);
        sme.ReorderCandidatesAfterDeletion(reference.Position);
    }

    private MajorityElectionCandidate FindCandidate(Guid candidateId)
    {
        return Candidates.SingleOrDefault(c => c.Id == candidateId)
            ?? throw new ValidationException($"Candidate {candidateId} does not exist");
    }

    private void EnsureUniqueCandidatePosition(MajorityElectionCandidate changedCandidate)
    {
        var position = changedCandidate.Position;

        if (Candidates.Any(c => c.Id != changedCandidate.Id && c.Position == position))
        {
            throw new ValidationException($"Candidate position {position} is already taken.");
        }
    }

    private void EnsureCandidateExists(Guid candidateId)
    {
        if (Candidates.All(c => c.Id != candidateId))
        {
            throw new ValidationException($"Candidate {candidateId} does not exist");
        }
    }

    private void EnsureUniqueCandidateNumber(MajorityElectionCandidate candidate)
    {
        if (Candidates.Any(c => c.Id != candidate.Id && c.Number == candidate.Number))
        {
            throw new NonUniqueCandidateNumberException();
        }
    }

    private SecondaryMajorityElection GetSecondaryMajorityElection(Guid id)
    {
        return SecondaryMajorityElections.Find(s => s.Id == id)
            ?? throw new ValidationException($"Secondary majority election {id} does not exist");
    }

    private MajorityElectionBallotGroup GetBallotGroup(Guid id)
    {
        return BallotGroups.Find(s => s.Id == id)
            ?? throw new ValidationException($"Ballot group {id} does not exist");
    }

    private void EnsureValidCreateUpdateBallotGroupEntries(IReadOnlyCollection<MajorityElectionBallotGroupEntry> entries)
    {
        var electionIds = entries.Select(e => e.ElectionId).ToHashSet();
        if (entries.Count != electionIds.Count
            || !electionIds.Remove(Id)
            || !SecondaryMajorityElections.All(sme => sme.IsOnSeparateBallot || electionIds.Remove(sme.Id)))
        {
            throw new ValidationException("A ballot group should contain all elections exactly once");
        }

        if (electionIds.Count != 0)
        {
            throw new ValidationException("A ballot group should contain all elections exactly once");
        }

        if (entries.Any(e => e.BlankRowCount != 0))
        {
            throw new ValidationException("Cannot set blank row count on create or update ballot group");
        }
    }

    private void EnsureBallotGroupBlankRowsAreNotOk(MajorityElectionBallotGroup ballotGroup)
    {
        foreach (var entry in ballotGroup.Entries)
        {
            var numberOfMandates = Id == entry.ElectionId
                ? NumberOfMandates
                : GetSecondaryMajorityElection(entry.ElectionId).NumberOfMandates;

            if (!entry.CandidateCountOk(numberOfMandates))
            {
                return;
            }
        }

        throw new ValidationException("The candidate count for this ballot group is correct, modifications aren't allowed anymore.");
    }

    private void EnsureBallotGroupEntriesAreOk(MajorityElectionBallotGroup ballotGroup)
    {
        var electionsCount = SecondaryMajorityElectionsOnSameBallot.Count + 1;
        if (electionsCount != ballotGroup.Entries.Count)
        {
            throw new MajorityElectionBallotGroupVoteCountException(ballotGroup.Id);
        }

        foreach (var entry in ballotGroup.Entries)
        {
            var numberOfMandates = Id == entry.ElectionId
                ? NumberOfMandates
                : GetSecondaryMajorityElection(entry.ElectionId).NumberOfMandates;

            if (!entry.CandidateCountOk(numberOfMandates))
            {
                throw new MajorityElectionBallotGroupVoteCountException(ballotGroup.Id);
            }
        }
    }

    private void EnsureBallotGroupCandidateEntriesAreOk(MajorityElectionBallotGroupCandidates ballotGroupCandidates)
    {
        var ballotGroup = GetBallotGroup(ballotGroupCandidates.BallotGroupId);
        var ballotGroupCandidatesEntryIds = ballotGroupCandidates.EntryCandidates.ConvertAll(c => c.BallotGroupEntryId);

        foreach (var entry in ballotGroupCandidates.EntryCandidates)
        {
            var ballotGroupEntry = ballotGroup.Entries.Single(e => e.Id == entry.BallotGroupEntryId);
            var electionId = ballotGroupEntry.ElectionId;

            if (electionId == Id
                && NumberOfMandates == 1
                && SecondaryMajorityElectionsOnSameBallot.Count == 0
                && entry.BlankRowCount != 0)
            {
                throw new ValidationException("Cannot set blank row count on a single mandate election without secondary elections on the same ballot");
            }

            var numberOfMandates = Id == electionId
                ? NumberOfMandates
                : GetSecondaryMajorityElection(electionId).NumberOfMandates;

            if (numberOfMandates != entry.CandidateIds.Count + entry.IndividualCandidatesVoteCount + entry.BlankRowCount)
            {
                throw new MajorityElectionBallotGroupVoteCountException(ballotGroup.Id);
            }
        }

        var missingBallotGroupEntries = ballotGroup.Entries.Where(e => !ballotGroupCandidatesEntryIds.Contains(e.Id)).ToList();

        if (ballotGroupCandidates.EntryCandidates.Any(e => e.IndividualCandidatesVoteCount + e.CandidateIds.Count > 0)
            || missingBallotGroupEntries.Any(e => e.IndividualCandidatesVoteCount + e.CandidateIds.Count > 0))
        {
            return;
        }

        throw new ValidationException("A ballot group cannot contain only blank rows");
    }

    private void EnsureCandidateIsValid(
        MajorityElectionCandidate candidate,
        MajorityElectionCandidate? existingCandidate,
        CandidateValidationParams candidateValidationParams,
        bool individualCandidatesDisabled)
    {
        EnsureValidCandidateReportingType(
            candidate,
            existingCandidate,
            candidateValidationParams.TestingPhaseEnded,
            individualCandidatesDisabled,
            c => c?.ReportingType);

        if (candidateValidationParams.OnlyNamesAndNumberRequired == true)
        {
            return;
        }

        if (!candidate.DateOfBirth.HasValue)
        {
            throw new ValidationException("Date of birth is required during testing phase.");
        }

        if (candidate.Sex == Data.Models.SexType.Unspecified)
        {
            throw new ValidationException("Sex is required during testing phase.");
        }

        if (candidateValidationParams.IsLocalityRequired && string.IsNullOrEmpty(candidate.Locality) && !candidateValidationParams.DoiType.IsCommunal())
        {
            throw new ValidationException("Candidate locality is required for non communal political businesses during testing phase.");
        }

        if (candidateValidationParams.IsOriginRequired && string.IsNullOrEmpty(candidate.Origin) && !candidateValidationParams.DoiType.IsCommunal())
        {
            throw new ValidationException("Candidate origin is required for non communal political businesses during testing phase.");
        }

        if (!candidate.PartyShortDescription.Keys.OrderBy(x => x).SequenceEqual(Languages.All.OrderBy(x => x)))
        {
            throw new ValidationException("Party is required during testing phase.");
        }

        if (!candidate.PartyLongDescription.Keys.OrderBy(x => x).SequenceEqual(Languages.All.OrderBy(x => x)))
        {
            throw new ValidationException("Party is required during testing phase.");
        }
    }

    private void EnsureValidCandidateReportingType<T>(
        T data,
        T? existingData,
        bool testingPhaseEnded,
        bool individualCandidatesDisabled,
        Func<T?, MajorityElectionCandidateReportingType?> selector)
    {
        var newReportingType = selector(data)!.Value;

        if (individualCandidatesDisabled)
        {
            if (newReportingType is MajorityElectionCandidateReportingType.Unspecified)
            {
                return;
            }

            throw new ValidationException("Cannot set reporting type if individual candidates are disabled");
        }

        var existingReportingType = selector(existingData);
        var existingCandidateCreatedBeforeTestingPhaseEnded = existingReportingType is MajorityElectionCandidateReportingType.Unspecified;

        if (existingReportingType is MajorityElectionCandidateReportingType.Unspecified
            && newReportingType is MajorityElectionCandidateReportingType.Unspecified)
        {
            return;
        }

        if (!testingPhaseEnded)
        {
            if (newReportingType is not MajorityElectionCandidateReportingType.Unspecified)
            {
                throw new ValidationException("Candidate reporting type cannot be set during testing phase");
            }
        }
        else
        {
            if (existingCandidateCreatedBeforeTestingPhaseEnded
                && newReportingType is not MajorityElectionCandidateReportingType.Unspecified)
            {
                throw new ValidationException("Candidate created before testing phase ended cannot set a reporting type");
            }

            if (!existingCandidateCreatedBeforeTestingPhaseEnded
                && newReportingType is MajorityElectionCandidateReportingType.Unspecified)
            {
                throw new ValidationException("Candidates created after the testing phase must have a reporting type");
            }
        }
    }

    private void EnsureIsNotInBallotGroup(Guid candidateId)
    {
        var allCandidateIdsInBallotGroups = BallotGroups
            .SelectMany(x => x.Entries)
            .SelectMany(x => x.CandidateIds);
        if (allCandidateIdsInBallotGroups.Any(x => x == candidateId.ToString()))
        {
            throw new MajorityElectionCandidateIsInBallotGroupException(candidateId);
        }
    }

    private void EnsureValidExistingBallotGroups()
    {
        foreach (var ballotGroup in BallotGroups)
        {
            EnsureBallotGroupEntriesAreOk(ballotGroup);
        }
    }

    private bool AnyElectionIsActive() => Active || SecondaryMajorityElections.Any(m => m.Active);

    private void EnsureSelectedCandidatesAreSelectedInPrimaryElection(MajorityElectionBallotGroupCandidates data)
    {
        var ballotGroup = BallotGroups.Single(bg => bg.Id == data.BallotGroupId);
        var primaryBallotGroupEntry = ballotGroup.Entries.Single(e => e.ElectionId == Id);

        var selectedPrimaryCandidateIds = data.EntryCandidates.SingleOrDefault(c => c.BallotGroupEntryId == primaryBallotGroupEntry.Id)?.CandidateIds
            ?? primaryBallotGroupEntry.CandidateIds.ConvertAll(Guid.Parse);

        foreach (var secondaryEntryCandidates in data.EntryCandidates.Where(c => c.BallotGroupEntryId != primaryBallotGroupEntry.Id))
        {
            var secondaryBallotGroupEntry = ballotGroup.Entries.Single(e => e.Id == secondaryEntryCandidates.BallotGroupEntryId);
            var secondaryElection = SecondaryMajorityElections.Single(sme => sme.Id == secondaryBallotGroupEntry.ElectionId);

            var candidateIdByRefId = secondaryElection.CandidateReferences.ToDictionary(c => c.Id, c => c.CandidateId);

            if (secondaryElection.IsOnSeparateBallot)
            {
                continue;
            }

            foreach (var refId in secondaryEntryCandidates.CandidateIds)
            {
                if (candidateIdByRefId.TryGetValue(refId, out var candidateId) && !selectedPrimaryCandidateIds.Contains(candidateId))
                {
                    throw new SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException();
                }
            }
        }
    }

    private void EnsureCanSetActive(bool active)
    {
        EnsureNotDeleted();

        if (active)
        {
            EnsureValidExistingBallotGroups();
        }
    }

    private void EnsureEVotingNotApproved()
    {
        if (EVotingApproved == true)
        {
            throw new PoliticalBusinessEVotingApprovedException();
        }
    }

    private void EnsureEVotingNotApproved(Guid smeId)
    {
        var sme = GetSecondaryMajorityElection(smeId);

        if (sme.EVotingApproved == true)
        {
            throw new PoliticalBusinessEVotingApprovedException();
        }
    }
}
