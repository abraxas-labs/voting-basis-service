// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Shared.V1;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Core.Utils;
using Voting.Lib.Common;
using BallotNumberGeneration = Voting.Basis.Data.Models.BallotNumberGeneration;
using DomainOfInfluenceType = Voting.Basis.Data.Models.DomainOfInfluenceType;
using MajorityElectionMandateAlgorithm = Voting.Basis.Data.Models.MajorityElectionMandateAlgorithm;
using MajorityElectionResultEntry = Voting.Basis.Data.Models.MajorityElectionResultEntry;
using MajorityElectionReviewProcedure = Voting.Basis.Data.Models.MajorityElectionReviewProcedure;
using SexType = Abraxas.Voting.Basis.Shared.V1.SexType;

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
        _validator.ValidateAndThrow(majorityElection);

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
        _validator.ValidateAndThrow(majorityElection);

        // Active shouldn't be changed by updates after the testing phase, but also shouldn't throw an error,
        // since sometimes the wrong values is provided, which would result in a "modified exception"
        majorityElection.Active = Active;

        ValidationUtils.EnsureNotModified(NumberOfMandates, majorityElection.NumberOfMandates);
        ValidationUtils.EnsureNotModified(MandateAlgorithm, majorityElection.MandateAlgorithm);
        ValidationUtils.EnsureNotModified(CandidateCheckDigit, majorityElection.CandidateCheckDigit);
        ValidationUtils.EnsureNotModified(BallotBundleSize, majorityElection.BallotBundleSize);
        ValidationUtils.EnsureNotModified(AutomaticBallotBundleNumberGeneration, majorityElection.AutomaticBallotBundleNumberGeneration);
        ValidationUtils.EnsureNotModified(BallotNumberGeneration, majorityElection.BallotNumberGeneration);
        ValidationUtils.EnsureNotModified(AutomaticEmptyVoteCounting, majorityElection.AutomaticEmptyVoteCounting);
        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, majorityElection.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(ContestId, majorityElection.ContestId);
        ValidationUtils.EnsureNotModified(BallotBundleSampleSize, majorityElection.BallotBundleSampleSize);
        ValidationUtils.EnsureNotModified(ResultEntry, majorityElection.ResultEntry);
        ValidationUtils.EnsureNotModified(ReviewProcedure, majorityElection.ReviewProcedure);
        ValidationUtils.EnsureNotModified(EnforceReviewProcedureForCountingCircles, majorityElection.EnforceReviewProcedureForCountingCircles);

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
        EnsureNotDeleted();
        var ev = new MajorityElectionActiveStateUpdated
        {
            MajorityElectionId = Id.ToString(),
            Active = active,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void Delete()
    {
        EnsureNotDeleted();

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

    public void CreateCandidateFrom(MajorityElectionCandidate candidate, DomainOfInfluenceType doiType)
    {
        EnsureNotDeleted();
        if (candidate.Id == default)
        {
            candidate.Id = Guid.NewGuid();
        }

        _candidateValidator.ValidateAndThrow(candidate);

        if (candidate.Position != Candidates.Count + 1)
        {
            throw new ValidationException("Candidate position should be continuous");
        }

        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(candidate, doiType);
        EnsureUniqueCandidatePosition(candidate);
        EnsureUniqueCandidateNumber(candidate);

        var ev = new MajorityElectionCandidateCreated
        {
            MajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(candidate),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateFrom(MajorityElectionCandidate candidate, DomainOfInfluenceType doiType)
    {
        EnsureNotDeleted();
        _candidateValidator.ValidateAndThrow(candidate);

        var existingCandidate = FindCandidate(candidate.Id);
        if (candidate.Position != existingCandidate.Position)
        {
            throw new ValidationException("Cannot change the candidate position via an update");
        }

        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(candidate, doiType);
        EnsureUniqueCandidatePosition(candidate);
        EnsureUniqueCandidateNumber(candidate);

        var ev = new MajorityElectionCandidateUpdated
        {
            MajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(candidate),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateAfterTestingPhaseEnded(MajorityElectionCandidate candidate, DomainOfInfluenceType doiType)
    {
        EnsureNotDeleted();
        _candidateValidator.ValidateAndThrow(candidate);

        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(candidate, doiType);

        var existingCandidate = FindCandidate(candidate.Id)
            ?? throw new ValidationException($"Candidate {candidate.Id} does not exist");

        ValidationUtils.EnsureNotModified(existingCandidate.Number, candidate.Number);
        ValidationUtils.EnsureNotModified(existingCandidate.Position, candidate.Position);

        var ev = new MajorityElectionCandidateAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = candidate.Id.ToString(),
            MajorityElectionId = Id.ToString(),
            FirstName = candidate.FirstName,
            LastName = candidate.LastName,
            PoliticalFirstName = candidate.PoliticalFirstName,
            PoliticalLastName = candidate.PoliticalLastName,
            DateOfBirth = candidate.DateOfBirth.ToTimestamp(),
            Sex = _mapper.Map<SexType>(candidate.Sex),
            Occupation = { candidate.Occupation },
            Title = candidate.Title,
            OccupationTitle = { candidate.OccupationTitle },
            Incumbent = candidate.Incumbent,
            ZipCode = candidate.ZipCode,
            Locality = candidate.Locality,
            Party = { candidate.Party },
            Origin = candidate.Origin,
        };

        RaiseEvent(ev);
    }

    public void ReorderCandidates(IReadOnlyCollection<EntityOrder> orders)
    {
        EnsureNotDeleted();
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
        EnsureCandidateExists(candidateId);

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

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
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

        var sme = GetSecondaryMajorityElection(data.Id);

        if (data.AllowedCandidates == SecondaryMajorityElectionAllowedCandidates.MustExistInPrimaryElection && sme.Candidates.Count > 0)
        {
            throw new ValidationException("Non-primary election candidates exist, cannot change allowed candidates");
        }

        if (data.AllowedCandidates == SecondaryMajorityElectionAllowedCandidates.MustNotExistInPrimaryElection
            && sme.CandidateReferences.Count > 0)
        {
            throw new ValidationException("Candidate references exist, cannot change allowed candidates");
        }

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

        var sme = GetSecondaryMajorityElection(data.Id);

        // Active shouldn't be changed by updates after the testing phase, but also shouldn't throw an error,
        // since sometimes the wrong values is provided, which would result in a "modified exception"
        data.Active = sme.Active;

        ValidationUtils.EnsureNotModified(sme.NumberOfMandates, data.NumberOfMandates);
        ValidationUtils.EnsureNotModified(sme.AllowedCandidates, data.AllowedCandidates);
        ValidationUtils.EnsureNotModified(sme.Active, data.Active);

        var ev = new SecondaryMajorityElectionAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = data.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            OfficialDescription = { data.OfficialDescription },
            ShortDescription = { data.ShortDescription },
            PoliticalBusinessNumber = data.PoliticalBusinessNumber,
        };

        RaiseEvent(ev);
    }

    public void DeleteSecondaryMajorityElection(Guid id)
    {
        EnsureNotDeleted();
        GetSecondaryMajorityElection(id);

        var ev = new SecondaryMajorityElectionDeleted
        {
            SecondaryMajorityElectionId = id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionActiveState(Guid id, bool active)
    {
        EnsureNotDeleted();
        GetSecondaryMajorityElection(id);

        var ev = new SecondaryMajorityElectionActiveStateUpdated
        {
            SecondaryMajorityElectionId = id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            Active = active,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateSecondaryMajorityElectionCandidateFrom(MajorityElectionCandidate data, DomainOfInfluenceType doiType)
    {
        EnsureNotDeleted();

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
        }

        _candidateValidator.ValidateAndThrow(data);
        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(data, doiType);

        var sme = GetSecondaryMajorityElection(data.MajorityElectionId);

        if (sme.AllowedCandidates == SecondaryMajorityElectionAllowedCandidates.MustExistInPrimaryElection)
        {
            throw new ValidationException("Candidate must exist in primary election");
        }

        if (sme.AllowedCandidates == SecondaryMajorityElectionAllowedCandidates.MustNotExistInPrimaryElection
            && Candidates.Any(c => c.FirstName == data.FirstName && c.LastName == data.LastName && c.DateOfBirth.Date == data.DateOfBirth.Date))
        {
            throw new ValidationException("Candidate must not exist in primary election");
        }

        sme.EnsureValidCandidatePosition(data, true);
        var referencedCandidates = sme.CandidateReferences.ConvertAll(r => FindCandidate(r.CandidateId));
        sme.EnsureUniqueCandidateNumber(data, referencedCandidates);

        var ev = new SecondaryMajorityElectionCandidateCreated
        {
            SecondaryMajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(data),
            PrimaryMajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionCandidateFrom(MajorityElectionCandidate data, DomainOfInfluenceType doiType)
    {
        EnsureNotDeleted();
        _candidateValidator.ValidateAndThrow(data);
        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(data, doiType);

        var sme = GetSecondaryMajorityElection(data.MajorityElectionId);

        // ensure candidate exists
        sme.GetCandidate(data.Id);
        sme.EnsureValidCandidatePosition(data, false);
        var referencedCandidates = sme.CandidateReferences.ConvertAll(r => FindCandidate(r.CandidateId));
        sme.EnsureUniqueCandidateNumber(data, referencedCandidates);

        var ev = new SecondaryMajorityElectionCandidateUpdated
        {
            SecondaryMajorityElectionCandidate = _mapper.Map<MajorityElectionCandidateEventData>(data),
            PrimaryMajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateSecondaryMajorityElectionCandidateAfterTestingPhaseEnded(MajorityElectionCandidate data, DomainOfInfluenceType doiType)
    {
        EnsureNotDeleted();
        _candidateValidator.ValidateAndThrow(data);

        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(data, doiType);

        var sme = GetSecondaryMajorityElection(data.MajorityElectionId);
        var existingCandidate = sme.GetCandidate(data.Id);

        ValidationUtils.EnsureNotModified(existingCandidate.Number, data.Number);
        ValidationUtils.EnsureNotModified(existingCandidate.Position, data.Position);

        var ev = new SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = data.Id.ToString(),
            SecondaryMajorityElectionId = sme.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            FirstName = data.FirstName,
            LastName = data.LastName,
            PoliticalFirstName = data.PoliticalFirstName,
            PoliticalLastName = data.PoliticalLastName,
            DateOfBirth = data.DateOfBirth.ToTimestamp(),
            Sex = _mapper.Map<SexType>(data.Sex),
            Occupation = { data.Occupation },
            Title = data.Title,
            OccupationTitle = { data.OccupationTitle },
            Incumbent = data.Incumbent,
            ZipCode = data.ZipCode,
            Locality = data.Locality,
            Party = { data.Party },
            Origin = data.Origin,
        };

        RaiseEvent(ev);
    }

    public void DeleteSecondaryMajorityElectionCandidate(Guid secondaryMajorityElectionId, Guid id)
    {
        EnsureNotDeleted();

        var sme = GetSecondaryMajorityElection(secondaryMajorityElectionId);

        // ensure candidate exists
        sme.GetCandidate(id);

        var ev = new SecondaryMajorityElectionCandidateDeleted
        {
            SecondaryMajorityElectionCandidateId = id.ToString(),
            SecondaryMajorityElectionId = sme.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateCandidateReferenceFrom(MajorityElectionCandidateReference data)
    {
        EnsureNotDeleted();

        if (data.Id == default)
        {
            data.Id = Guid.NewGuid();
        }

        var sme = GetSecondaryMajorityElection(data.SecondaryMajorityElectionId);

        if (sme.AllowedCandidates == SecondaryMajorityElectionAllowedCandidates.MustNotExistInPrimaryElection)
        {
            throw new ValidationException("Candidate must not exist in primary election");
        }

        if (sme.CandidateReferences.Any(cr => cr.CandidateId == data.CandidateId))
        {
            throw new ValidationException("Candidate reference already exists");
        }

        var referencedCandidate = FindCandidate(data.CandidateId);
        sme.EnsureValidCandidatePosition(data, true);
        var referencedCandidates = sme.CandidateReferences.ConvertAll(r => FindCandidate(r.CandidateId));
        sme.EnsureUniqueCandidateNumber(data, referencedCandidates, referencedCandidate.Number);

        var candidateReferenceEventData = _mapper.Map<MajorityElectionCandidateReferenceEventData>(data);
        candidateReferenceEventData.PrimaryMajorityElectionId = Id.ToString();

        var ev = new SecondaryMajorityElectionCandidateReferenceCreated
        {
            MajorityElectionCandidateReference = candidateReferenceEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateReferenceFrom(MajorityElectionCandidateReference data)
    {
        EnsureNotDeleted();
        var sme = GetSecondaryMajorityElection(data.SecondaryMajorityElectionId);
        var existingReference = sme.GetCandidateReference(data.Id);

        if (data.CandidateId != existingReference.CandidateId)
        {
            throw new ValidationException($"{nameof(data.CandidateId)} is immutable");
        }

        var referencedCandidate = FindCandidate(data.CandidateId);
        sme.EnsureValidCandidatePosition(data, false);
        var referencedCandidates = sme.CandidateReferences.ConvertAll(r => FindCandidate(r.CandidateId));
        sme.EnsureUniqueCandidateNumber(data, referencedCandidates, referencedCandidate.Number);

        var candidateReferenceEventData = _mapper.Map<MajorityElectionCandidateReferenceEventData>(data);
        candidateReferenceEventData.PrimaryMajorityElectionId = Id.ToString();

        var ev = new SecondaryMajorityElectionCandidateReferenceUpdated
        {
            MajorityElectionCandidateReference = candidateReferenceEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteCandidateReference(Guid secondaryMajorityElectionId, Guid id)
    {
        EnsureNotDeleted();

        var sme = GetSecondaryMajorityElection(secondaryMajorityElectionId);
        sme.GetCandidateReference(id);

        var ev = new SecondaryMajorityElectionCandidateReferenceDeleted
        {
            SecondaryMajorityElectionCandidateReferenceId = id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void ReorderSecondaryMajorityElectionCandidates(Guid secondaryMajorityElectionId, IReadOnlyCollection<EntityOrder> orders)
    {
        EnsureNotDeleted();
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
            CandidateOrders = _mapper.Map<EntityOrdersEventData>(orders),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateBallotGroupFrom(MajorityElectionBallotGroup data)
    {
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

        EnsureNotDeleted();
        _ballotGroupValidator.ValidateAndThrow(data);
        EnsureCorrectBallotGroupEntries(data.Entries);

        if (BallotGroups.Any(bg => bg.Id != data.Id && bg.Position == data.Position))
        {
            throw new ValidationException($"Ballot group position {data.Position} is already taken.");
        }

        if (data.Position > BallotGroups.Count + 1)
        {
            throw new ValidationException($"The ballot group position {data.Position} is invalid, is non-continuous.");
        }

        var ev = new MajorityElectionBallotGroupCreated
        {
            BallotGroup = _mapper.Map<MajorityElectionBallotGroupEventData>(data),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateBallotGroup(MajorityElectionBallotGroup data, bool testingPhaseEnded)
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
        EnsureCorrectBallotGroupEntries(data.Entries);
        var ballotGroup = GetBallotGroup(data.Id);

        if (data.Position != ballotGroup.Position)
        {
            throw new ValidationException("Cannot change the ballot group position via an update");
        }

        if (ballotGroup.Entries.Any(e => data.Entries.Any(newE => newE.ElectionId == e.ElectionId && newE.Id != e.Id)))
        {
            throw new ValidationException("Cannot change the id of a ballot group entry");
        }

        if (testingPhaseEnded)
        {
            // If the contest testing phase is over, only ballot groups with invalid configurations (candidate count is not ok) can be modified
            EnsureBallotGroupBlankRowsAreNotOk(ballotGroup);
        }

        var ev = new MajorityElectionBallotGroupUpdated
        {
            BallotGroup = _mapper.Map<MajorityElectionBallotGroupEventData>(data),
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

    public void UpdateElectionGroupDescription(string description)
    {
        EnsureNotDeleted();

        if (string.IsNullOrEmpty(description))
        {
            throw new ValidationException("Election group description cannot be empty");
        }

        if (ElectionGroup == null)
        {
            throw new ValidationException("Election group does not exist");
        }

        var ev = new ElectionGroupUpdated
        {
            ElectionGroupId = ElectionGroup.Id.ToString(),
            PrimaryMajorityElectionId = Id.ToString(),
            Description = description,
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
            case ElectionGroupUpdated e:
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
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.MajorityElection.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.MajorityElectionReviewProcedure.Unspecified)
        {
            ev.MajorityElection.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.MajorityElectionReviewProcedure.Electronically;
        }

        _mapper.Map(ev.MajorityElection, this);
    }

    private void Apply(MajorityElectionUpdated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.MajorityElection.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.MajorityElectionReviewProcedure.Unspecified)
        {
            ev.MajorityElection.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.MajorityElectionReviewProcedure.Electronically;
        }

        _mapper.Map(ev.MajorityElection, this);
    }

    private void Apply(MajorityElectionAfterTestingPhaseUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(MajorityElectionActiveStateUpdated ev)
    {
        Active = ev.Active;
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

    private void Apply(ElectionGroupUpdated ev)
    {
        ElectionGroup!.Description = ev.Description;
    }

    private void Apply(MajorityElectionBallotGroupCreated ev)
    {
        var ballotGroup = _mapper.Map<MajorityElectionBallotGroup>(ev.BallotGroup);
        BallotGroups.Add(ballotGroup);
    }

    private void Apply(MajorityElectionBallotGroupUpdated ev)
    {
        var ballotGroup = GetBallotGroup(GuidParser.Parse(ev.BallotGroup.Id));
        _mapper.Map(ev.BallotGroup, ballotGroup);
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
    }

    private void Apply(SecondaryMajorityElectionActiveStateUpdated ev)
    {
        var sme = GetSecondaryMajorityElection(GuidParser.Parse(ev.SecondaryMajorityElectionId));
        sme.Active = ev.Active;
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

    private void EnsureCorrectBallotGroupEntries(IReadOnlyCollection<MajorityElectionBallotGroupEntry> entries)
    {
        if (entries.Count != SecondaryMajorityElections.Count + 1
            || !SecondaryMajorityElections.All(sme => entries.Any(e => e.ElectionId == sme.Id))
            || entries.All(e => e.ElectionId != Id))
        {
            throw new ValidationException("A ballot group should contain all elections exactly once");
        }

        foreach (var entry in entries)
        {
            if (entry.ElectionId == Id && entry.BlankRowCount > NumberOfMandates)
            {
                throw new ValidationException("The ballot group cannot have more blank rows than number of mandates");
            }

            var sme = SecondaryMajorityElections.Find(e => e.Id == entry.ElectionId);
            if (sme != null && entry.BlankRowCount > sme.NumberOfMandates)
            {
                throw new ValidationException("The ballot group cannot have more blank rows than number of mandates");
            }
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

    private void EnsureLocalityAndOriginIsSetForNonCommunalDoiType(MajorityElectionCandidate candidate, DomainOfInfluenceType doiType)
    {
        if (string.IsNullOrEmpty(candidate.Locality) && !doiType.IsCommunal())
        {
            throw new ValidationException("Candidate locality is required for non communal political businesses");
        }

        if (string.IsNullOrEmpty(candidate.Origin) && !doiType.IsCommunal())
        {
            throw new ValidationException("Candidate origin is required for non communal political businesses");
        }
    }
}
