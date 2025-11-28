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
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="ProportionalElection"/>.
/// </summary>
public class ProportionalElectionAggregate : BaseHasContestAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<ProportionalElection> _validator;
    private readonly IValidator<IEnumerable<EntityOrder>> _entityOrdersValidator;
    private readonly IValidator<ProportionalElectionCandidate> _candidateValidator;

    public ProportionalElectionAggregate(
        IMapper mapper,
        EventInfoProvider eventInfoProvider,
        IValidator<ProportionalElection> validator,
        IValidator<IEnumerable<EntityOrder>> entityOrdersValidator,
        IValidator<ProportionalElectionCandidate> candidateValidator)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
        _entityOrdersValidator = entityOrdersValidator;
        _candidateValidator = candidateValidator;
        PoliticalBusinessNumber = string.Empty;
        OfficialDescription = new Dictionary<string, string>();
        ShortDescription = new Dictionary<string, string>();

        Lists = new List<ProportionalElectionList>();
        ListUnions = new List<ProportionalElectionListUnion>();
    }

    public override string AggregateName => "voting-proportionalElections";

    public string PoliticalBusinessNumber { get; private set; }

    public Dictionary<string, string> OfficialDescription { get; private set; }

    public Dictionary<string, string> ShortDescription { get; private set; }

    public int NumberOfMandates { get; private set; }

    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; private set; }

    public bool CandidateCheckDigit { get; private set; }

    public int BallotBundleSize { get; private set; }

    public int BallotBundleSampleSize { get; private set; }

    public bool AutomaticBallotBundleNumberGeneration { get; private set; }

    public BallotNumberGeneration BallotNumberGeneration { get; private set; }

    public bool AutomaticEmptyVoteCounting { get; private set; }

    public bool EnforceEmptyVoteCountingForCountingCircles { get; private set; }

    public Guid DomainOfInfluenceId { get; private set; }

    public bool Active { get; private set; }

    public List<ProportionalElectionList> Lists { get; private set; }

    public List<ProportionalElectionListUnion> ListUnions { get; private set; }

    public ProportionalElectionReviewProcedure ReviewProcedure { get; private set; }

    public bool EnforceReviewProcedureForCountingCircles { get; private set; }

    public bool EnforceCandidateCheckDigitForCountingCircles { get; private set; }

    public int? FederalIdentification { get; set; }

    public bool? EVotingApproved { get; private set; }

    public void CreateFrom(ProportionalElection proportionalElection)
    {
        if (proportionalElection.Id == default)
        {
            proportionalElection.Id = Guid.NewGuid();
        }

        _validator.ValidateAndThrow(proportionalElection);

        var ev = new ProportionalElectionCreated
        {
            ProportionalElection = _mapper.Map<ProportionalElectionEventData>(proportionalElection),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(proportionalElection.ContestId));
    }

    public void UpdateFrom(ProportionalElection proportionalElection)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _validator.ValidateAndThrow(proportionalElection);

        // We only set a different e-voting approved on create or approval update.
        proportionalElection.EVotingApproved = EVotingApproved;

        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, proportionalElection.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(MandateAlgorithm, proportionalElection.MandateAlgorithm);

        if (Active)
        {
            ValidationUtils.EnsureNotModified(NumberOfMandates, proportionalElection.NumberOfMandates);
        }

        var ev = new ProportionalElectionUpdated
        {
            ProportionalElection = _mapper.Map<ProportionalElectionEventData>(proportionalElection),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateAfterTestingPhaseEnded(ProportionalElection proportionalElection)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _validator.ValidateAndThrow(proportionalElection);

        // Active shouldn't be changed by updates after the testing phase, but also shouldn't throw an error,
        // since sometimes the wrong values is provided, which would result in a "modified exception"
        proportionalElection.Active = Active;

        ValidationUtils.EnsureNotModified(NumberOfMandates, proportionalElection.NumberOfMandates);
        ValidationUtils.EnsureNotModified(MandateAlgorithm, proportionalElection.MandateAlgorithm);
        ValidationUtils.EnsureNotModified(CandidateCheckDigit, proportionalElection.CandidateCheckDigit);
        ValidationUtils.EnsureNotModified(BallotBundleSize, proportionalElection.BallotBundleSize);
        ValidationUtils.EnsureNotModified(AutomaticBallotBundleNumberGeneration, proportionalElection.AutomaticBallotBundleNumberGeneration);
        ValidationUtils.EnsureNotModified(BallotNumberGeneration, proportionalElection.BallotNumberGeneration);
        ValidationUtils.EnsureNotModified(AutomaticEmptyVoteCounting, proportionalElection.AutomaticEmptyVoteCounting);
        ValidationUtils.EnsureNotModified(DomainOfInfluenceId, proportionalElection.DomainOfInfluenceId);
        ValidationUtils.EnsureNotModified(ContestId, proportionalElection.ContestId);
        ValidationUtils.EnsureNotModified(BallotBundleSampleSize, proportionalElection.BallotBundleSampleSize);
        ValidationUtils.EnsureNotModified(ReviewProcedure, proportionalElection.ReviewProcedure);
        ValidationUtils.EnsureNotModified(EnforceReviewProcedureForCountingCircles, proportionalElection.EnforceReviewProcedureForCountingCircles);
        ValidationUtils.EnsureNotModified(EnforceCandidateCheckDigitForCountingCircles, proportionalElection.EnforceCandidateCheckDigitForCountingCircles);
        ValidationUtils.EnsureNotModified(FederalIdentification, proportionalElection.FederalIdentification);

        var ev = new ProportionalElectionAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = proportionalElection.Id.ToString(),
            PoliticalBusinessNumber = proportionalElection.PoliticalBusinessNumber,
            OfficialDescription = { proportionalElection.OfficialDescription },
            ShortDescription = { proportionalElection.ShortDescription },
            EnforceEmptyVoteCountingForCountingCircles = proportionalElection.EnforceEmptyVoteCountingForCountingCircles,
        };

        RaiseEvent(ev);
    }

    public void UpdateActiveState(bool active)
    {
        EnsureCanSetActive(active);
        EnsureEVotingNotApproved();
        var ev = new ProportionalElectionActiveStateUpdated
        {
            ProportionalElectionId = Id.ToString(),
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
            throw new ValidationException($"Proportional election {Id} does not support E-Voting");
        }

        var ev = new ProportionalElectionEVotingApprovalUpdated
        {
            ProportionalElectionId = Id.ToString(),
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

    public void UpdateMandatAlgorithm(ProportionalElectionMandateAlgorithm mandateAlgorithm)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        var ev = new ProportionalElectionMandateAlgorithmUpdated
        {
            ProportionalElectionId = Id.ToString(),
            MandateAlgorithm = _mapper.Map<SharedProto.ProportionalElectionMandateAlgorithm>(mandateAlgorithm),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void Delete(bool ignoreCheck = false)
    {
        EnsureNotDeleted();

        if (!ignoreCheck)
        {
            EnsureEVotingNotApproved();
        }

        var ev = new ProportionalElectionDeleted
        {
            ProportionalElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateListFrom(ProportionalElectionList list)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        if (list.Id == default)
        {
            list.Id = Guid.NewGuid();
        }

        EnsureUniqueListPosition(list);

        if (list.BlankRowCount > NumberOfMandates)
        {
            throw new ValidationException("The BlankRowCount can't be greater than the NumberOfMandates");
        }

        var ev = new ProportionalElectionListCreated
        {
            ProportionalElectionList = _mapper.Map<ProportionalElectionListEventData>(list),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateListFrom(ProportionalElectionList list)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();

        var existingList = FindList(list.Id);
        EnsureUniqueListPosition(list);

        if (list.BlankRowCount > NumberOfMandates)
        {
            throw new ValidationException("The BlankRowCount can't be greater than the NumberOfMandates");
        }

        if (list.Position != existingList.Position)
        {
            throw new ValidationException("Cannot change the list position via an update");
        }

        var ev = new ProportionalElectionListUpdated
        {
            ProportionalElectionList = _mapper.Map<ProportionalElectionListEventData>(list),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateListAfterTestingPhaseEnded(ProportionalElectionList list)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();

        var existingList = FindList(list.Id);
        ValidationUtils.EnsureNotModified(existingList.OrderNumber, list.OrderNumber);
        ValidationUtils.EnsureNotModified(existingList.BlankRowCount, list.BlankRowCount);
        ValidationUtils.EnsureNotModified(existingList.Position, list.Position);

        var ev = new ProportionalElectionListAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = list.Id.ToString(),
            ProportionalElectionId = Id.ToString(),
            Description = { list.Description },
            ShortDescription = { list.ShortDescription },
        };

        RaiseEvent(ev);
    }

    public void ReorderLists(IReadOnlyCollection<EntityOrder> orders)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _entityOrdersValidator.ValidateAndThrow(orders);

        var ids = orders.Select(o => o.Id).ToArray();
        if (ids.Distinct().Count() != ids.Length)
        {
            throw new ValidationException("Duplicate list ids while reordering");
        }

        if (ids.Length != Lists.Count || Lists.Any(l => !ids.Contains(l.Id)))
        {
            throw new ValidationException("Not all list ids provided while reordering");
        }

        var ev = new ProportionalElectionListsReordered
        {
            ProportionalElectionId = Id.ToString(),
            ListOrders = _mapper.Map<EntityOrdersEventData>(orders),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteList(Guid listId)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureListExists(listId);
        EnsureListNotInUnion(listId);

        var ev = new ProportionalElectionListDeleted
        {
            ProportionalElectionListId = listId.ToString(),
            ProportionalElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateListUnionFrom(ProportionalElectionListUnion listUnion)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureIsHagenbachBischoffElection();
        if (listUnion.Id == default)
        {
            listUnion.Id = Guid.NewGuid();
        }

        EnsureValidListUnion(listUnion);

        var ev = new ProportionalElectionListUnionCreated
        {
            ProportionalElectionListUnion = _mapper.Map<ProportionalElectionListUnionEventData>(listUnion),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateListUnionFrom(ProportionalElectionListUnion listUnion)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureIsHagenbachBischoffElection();

        var existingListUnion = FindListUnion(listUnion.Id);
        if (listUnion.ProportionalElectionRootListUnionId != existingListUnion.ProportionalElectionRootListUnionId)
        {
            throw new ValidationException("Cannot change the ListUnion RootId via an update");
        }

        if (listUnion.Position != existingListUnion.Position)
        {
            throw new ValidationException("Cannot change the ListUnion Position via an update");
        }

        EnsureValidListUnion(listUnion);

        var ev = new ProportionalElectionListUnionUpdated
        {
            ProportionalElectionListUnion = _mapper.Map<ProportionalElectionListUnionEventData>(listUnion),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void ReorderListUnions(Guid? rootListUnionId, IReadOnlyCollection<EntityOrder> orders)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureIsHagenbachBischoffElection();

        _entityOrdersValidator.ValidateAndThrow(orders);

        var ids = orders.Select(o => o.Id).ToArray();
        if (ids.Distinct().Count() != ids.Length)
        {
            throw new ValidationException("Duplicate ListUnion Ids while reordering");
        }

        var listUnionsWithSameRoot = ListUnions.Where(lu => lu.ProportionalElectionRootListUnionId == rootListUnionId).ToArray();

        if (ids.Length != listUnionsWithSameRoot.Length || listUnionsWithSameRoot.Any(lu => !ids.Contains(lu.Id)))
        {
            throw new ValidationException("Not all ListUnion Ids provided while reordering");
        }

        var ev = new ProportionalElectionListUnionsReordered
        {
            ProportionalElectionId = Id.ToString(),
            ProportionalElectionRootListUnionId = rootListUnionId?.ToString() ?? string.Empty,
            ProportionalElectionListUnionOrders = _mapper.Map<EntityOrdersEventData>(orders),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteListUnion(Guid listUnionId)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureIsHagenbachBischoffElection();
        EnsureListUnionExists(listUnionId);

        var ev = new ProportionalElectionListUnionDeleted
        {
            ProportionalElectionListUnionId = listUnionId.ToString(),
            ProportionalElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateListUnionEntriesFrom(ProportionalElectionListUnionEntries listUnionEntries)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureIsHagenbachBischoffElection();
        var existingListUnion = FindListUnion(listUnionEntries.ProportionalElectionListUnionId);

        if (listUnionEntries.ProportionalElectionListIds.Distinct().Count() != listUnionEntries.ProportionalElectionListIds.Count)
        {
            throw new ValidationException("Duplicate ListIds in update ListUnionEntries");
        }

        var listIds = Lists.Select(l => l.Id).ToHashSet();
        if (listUnionEntries.ProportionalElectionListIds.Any(newListId => !listIds.Contains(newListId)))
        {
            throw new ValidationException("Only ListIds from the same ProportionalElection allowed");
        }

        if (existingListUnion.ProportionalElectionRootListUnionId.HasValue && listUnionEntries.ProportionalElectionListIds.Count > 0)
        {
            var rootListUnion = FindListUnion(existingListUnion.ProportionalElectionRootListUnionId.Value);
            if (listUnionEntries.ProportionalElectionListIds.Any(newListId => !rootListUnion.ProportionalElectionListIds.Contains(newListId)))
            {
                throw new ValidationException("SubListUnion may only contain Lists which are in the RootListUnion");
            }
        }

        if (listUnionEntries.ProportionalElectionListIds.Count < 2)
        {
            throw new ProportionalElectionListUnionMissingListsException(Id);
        }

        var listUnionEntriesEventData = _mapper.Map<ProportionalElectionListUnionEntriesEventData>(listUnionEntries);
        listUnionEntriesEventData.ProportionalElectionId = Id.ToString();

        var ev = new ProportionalElectionListUnionEntriesUpdated
        {
            ProportionalElectionListUnionEntries = listUnionEntriesEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateListUnionMainList(Guid unionId, Guid? mainListId)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        EnsureIsHagenbachBischoffElection();

        var listUnion = FindListUnion(unionId);
        var list = mainListId.HasValue
            ? FindList(mainListId.Value)
            : null;

        if (list != null && !listUnion.ProportionalElectionListIds.Contains(list.Id))
        {
            throw new ValidationException("Only assigned SubLists are allowed as a MainList");
        }

        var ev = new ProportionalElectionListUnionMainListUpdated
        {
            ProportionalElectionListUnionId = listUnion.Id.ToString(),
            ProportionalElectionMainListId = list?.Id.ToString() ?? string.Empty,
            ProportionalElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CreateCandidateFrom(ProportionalElectionCandidate candidate, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        if (candidate.Id == default)
        {
            candidate.Id = Guid.NewGuid();
        }

        _candidateValidator.ValidateAndThrow(candidate);
        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(candidate, candidateValidationParams);

        var list = FindList(candidate.ProportionalElectionListId);
        EnsureUniqueCandidatePosition(list, candidate);
        EnsureListHasSpace(list, candidate);
        EnsureUniqueCandidateNumber(list, candidate);

        candidate.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(list.OrderNumber + candidate.Number);

        var candidateEventData = _mapper.Map<ProportionalElectionCandidateEventData>(candidate);
        candidateEventData.ProportionalElectionId = Id.ToString();

        var ev = new ProportionalElectionCandidateCreated
        {
            ProportionalElectionCandidate = candidateEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateFrom(ProportionalElectionCandidate candidate, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _candidateValidator.ValidateAndThrow(candidate);
        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(candidate, candidateValidationParams);

        var list = FindList(candidate.ProportionalElectionListId);
        EnsureCandidateExists(list, candidate.Id);
        EnsureUniqueCandidatePosition(list, candidate);
        EnsureListHasSpace(list, candidate);
        EnsureUniqueCandidateNumber(list, candidate);

        candidate.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(list.OrderNumber + candidate.Number);

        var candidateEventData = _mapper.Map<ProportionalElectionCandidateEventData>(candidate);
        candidateEventData.ProportionalElectionId = Id.ToString();

        var ev = new ProportionalElectionCandidateUpdated
        {
            ProportionalElectionCandidate = candidateEventData,
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateCandidateAfterTestingPhaseEnded(ProportionalElectionCandidate candidate, CandidateValidationParams candidateValidationParams)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _candidateValidator.ValidateAndThrow(candidate);
        EnsureLocalityAndOriginIsSetForNonCommunalDoiType(candidate, candidateValidationParams);

        var list = FindList(candidate.ProportionalElectionListId);
        candidate.CheckDigit = ElectionCandidateCheckDigitUtils.CalculateCheckDigit(list.OrderNumber + candidate.Number);

        var existingCandidate = FindCandidate(candidate.ProportionalElectionListId, candidate.Id)
            ?? throw new ValidationException($"Candidate {candidate.Id} does not exist");

        ValidationUtils.EnsureNotModified(existingCandidate.Number, candidate.Number);
        ValidationUtils.EnsureNotModified(existingCandidate.CheckDigit, candidate.CheckDigit);
        ValidationUtils.EnsureNotModified(existingCandidate.Position, candidate.Position);
        ValidationUtils.EnsureNotModified(existingCandidate.Accumulated, candidate.Accumulated);
        ValidationUtils.EnsureNotModified(existingCandidate.AccumulatedPosition, candidate.AccumulatedPosition);
        ValidationUtils.EnsureNotModified(existingCandidate.PartyId, candidate.PartyId);

        var ev = new ProportionalElectionCandidateAfterTestingPhaseUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Id = candidate.Id.ToString(),
            ProportionalElectionListId = candidate.ProportionalElectionListId.ToString(),
            ProportionalElectionId = Id.ToString(),
            FirstName = candidate.FirstName,
            LastName = candidate.LastName,
            PoliticalFirstName = candidate.PoliticalFirstName,
            PoliticalLastName = candidate.PoliticalLastName,
            DateOfBirth = candidate.DateOfBirth.ToTimestamp(),
            Sex = _mapper.Map<SharedProto.SexType>(candidate.Sex),
            Occupation = { candidate.Occupation },
            Title = candidate.Title,
            OccupationTitle = { candidate.OccupationTitle },
            Incumbent = candidate.Incumbent,
            ZipCode = candidate.ZipCode,
            Locality = candidate.Locality,
            Origin = candidate.Origin,
            Country = candidate.Country,
            Street = candidate.Street,
            HouseNumber = candidate.HouseNumber,
        };

        RaiseEvent(ev);
    }

    public void ReorderCandidates(Guid listId, IEnumerable<EntityOrder> orders)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        _entityOrdersValidator.ValidateAndThrow(orders);
        var list = FindList(listId);
        var candidates = list.Candidates;

        var ids = orders.GroupBy(o => o.Id);
        if (ids.Any(id => id.Count() > 2))
        {
            throw new ValidationException("More than two positions for a candidate specified");
        }

        var providedSum = ids.Sum(i => i.Count());
        var neededSum = candidates.Sum(c => c.Accumulated ? 2 : 1);
        if (providedSum != neededSum)
        {
            throw new ValidationException("Not all candidate ids provided while reordering");
        }

        var ev = new ProportionalElectionCandidatesReordered
        {
            ProportionalElectionListId = listId.ToString(),
            CandidateOrders = _mapper.Map<EntityOrdersEventData>(orders),
            ProportionalElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void DeleteCandidate(Guid listId, Guid candidateId)
    {
        EnsureNotDeleted();
        EnsureEVotingNotApproved();
        var list = FindList(listId);
        EnsureCandidateExists(list, candidateId);

        var ev = new ProportionalElectionCandidateDeleted
        {
            ProportionalElectionCandidateId = candidateId.ToString(),
            ProportionalElectionListId = list.Id.ToString(),
            ProportionalElectionId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public override void MoveToNewContest(Guid newContestId)
    {
        EnsureNotDeleted();
        EnsureDifferentContest(newContestId);

        var ev = new ProportionalElectionToNewContestMoved
        {
            ProportionalElectionId = Id.ToString(),
            NewContestId = newContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, EventSignatureBusinessMetadataBuilder.BuildFrom(newContestId));
    }

    internal ProportionalElectionCandidate FindCandidate(Guid listId, Guid candidateId)
    {
        var list = FindList(listId);
        return list.Candidates.SingleOrDefault(c => c.Id == candidateId)
            ?? throw new ValidationException($"Candidate {candidateId} does not exist");
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionCreated e:
                Apply(e);
                break;
            case ProportionalElectionUpdated e:
                Apply(e);
                break;
            case ProportionalElectionAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case ProportionalElectionActiveStateUpdated e:
                Apply(e);
                break;
            case ProportionalElectionEVotingApprovalUpdated e:
                Apply(e);
                break;
            case ProportionalElectionDeleted _:
                Deleted = true;
                break;
            case ProportionalElectionToNewContestMoved e:
                ContestId = GuidParser.Parse(e.NewContestId);
                break;
            case ProportionalElectionListCreated e:
                Apply(e);
                break;
            case ProportionalElectionListUpdated e:
                Apply(e);
                break;
            case ProportionalElectionListAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case ProportionalElectionListsReordered e:
                Apply(e);
                break;
            case ProportionalElectionListDeleted e:
                Apply(e);
                break;
            case ProportionalElectionListUnionCreated e:
                Apply(e);
                break;
            case ProportionalElectionListUnionUpdated e:
                Apply(e);
                break;
            case ProportionalElectionListUnionsReordered e:
                Apply(e);
                break;
            case ProportionalElectionListUnionDeleted e:
                Apply(e);
                break;
            case ProportionalElectionListUnionEntriesUpdated e:
                Apply(e);
                break;
            case ProportionalElectionListUnionMainListUpdated e:
                Apply(e);
                break;
            case ProportionalElectionCandidateCreated e:
                Apply(e);
                break;
            case ProportionalElectionCandidateUpdated e:
                Apply(e);
                break;
            case ProportionalElectionCandidateAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case ProportionalElectionCandidatesReordered e:
                Apply(e);
                break;
            case ProportionalElectionCandidateDeleted e:
                Apply(e);
                break;
            case ProportionalElectionMandateAlgorithmUpdated e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(ProportionalElectionCreated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ProportionalElection.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.ProportionalElectionReviewProcedure.Unspecified)
        {
            ev.ProportionalElection.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.ProportionalElectionReviewProcedure.Electronically;
        }

        _mapper.Map(ev.ProportionalElection, this);
    }

    private void Apply(ProportionalElectionUpdated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ProportionalElection.ReviewProcedure == Abraxas.Voting.Basis.Shared.V1.ProportionalElectionReviewProcedure.Unspecified)
        {
            ev.ProportionalElection.ReviewProcedure = Abraxas.Voting.Basis.Shared.V1.ProportionalElectionReviewProcedure.Electronically;
        }

        _mapper.Map(ev.ProportionalElection, this);
    }

    private void Apply(ProportionalElectionAfterTestingPhaseUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(ProportionalElectionActiveStateUpdated ev)
    {
        Active = ev.Active;
    }

    private void Apply(ProportionalElectionEVotingApprovalUpdated ev)
    {
        EVotingApproved = ev.Approved;
    }

    private void Apply(ProportionalElectionListCreated ev)
    {
        var list = _mapper.Map<ProportionalElectionList>(ev.ProportionalElectionList);
        Lists.Add(list);
    }

    private void Apply(ProportionalElectionListUpdated ev)
    {
        var existingList = FindList(GuidParser.Parse(ev.ProportionalElectionList.Id));
        _mapper.Map(ev.ProportionalElectionList, existingList);
    }

    private void Apply(ProportionalElectionListAfterTestingPhaseUpdated ev)
    {
        var existingList = FindList(GuidParser.Parse(ev.Id));
        _mapper.Map(ev, existingList);
    }

    private void Apply(ProportionalElectionListsReordered ev)
    {
        foreach (var order in ev.ListOrders.Orders)
        {
            var list = FindList(GuidParser.Parse(order.Id));
            list.Position = order.Position;
        }
    }

    private void Apply(ProportionalElectionListDeleted ev)
    {
        var existingList = FindList(GuidParser.Parse(ev.ProportionalElectionListId));
        Lists.Remove(existingList);

        foreach (var list in Lists.Where(l => l.Position > existingList.Position))
        {
            list.Position--;
        }

        RemoveListUnionsForRemovedList(existingList.Id);
    }

    private void Apply(ProportionalElectionListUnionCreated ev)
    {
        var listUnion = _mapper.Map<ProportionalElectionListUnion>(ev.ProportionalElectionListUnion);
        ListUnions.Add(listUnion);
    }

    private void Apply(ProportionalElectionListUnionUpdated ev)
    {
        var existingListUnion = FindListUnion(GuidParser.Parse(ev.ProportionalElectionListUnion.Id));

        var listUnion = ev.ProportionalElectionListUnion;
        _mapper.Map(listUnion, existingListUnion);
    }

    private void Apply(ProportionalElectionListUnionsReordered ev)
    {
        foreach (var order in ev.ProportionalElectionListUnionOrders.Orders)
        {
            var listUnion = FindListUnion(GuidParser.Parse(order.Id));
            listUnion.Position = order.Position;
        }
    }

    private void Apply(ProportionalElectionListUnionDeleted ev)
    {
        var id = GuidParser.Parse(ev.ProportionalElectionListUnionId);
        RemoveListUnion(id);
    }

    private void Apply(ProportionalElectionListUnionEntriesUpdated ev)
    {
        var listUnion = FindListUnion(GuidParser.Parse(ev.ProportionalElectionListUnionEntries.ProportionalElectionListUnionId));
        var newListIds = ev.ProportionalElectionListUnionEntries.ProportionalElectionListIds.Select(GuidParser.Parse);

        listUnion.ProportionalElectionListIds.Clear();
        listUnion.ProportionalElectionListIds.AddRange(newListIds);

        if (listUnion.IsSubListUnion)
        {
            return;
        }

        foreach (var subListUnion in ListUnions.Where(lu => lu.ProportionalElectionRootListUnionId == listUnion.Id))
        {
            subListUnion.ProportionalElectionListIds = subListUnion.ProportionalElectionListIds.Where(listId => listUnion.ProportionalElectionListIds.Contains(listId)).ToList();
        }
    }

    private void Apply(ProportionalElectionListUnionMainListUpdated ev)
    {
        var existingListUnion = FindListUnion(GuidParser.Parse(ev.ProportionalElectionListUnionId));
        existingListUnion.ProportionalElectionMainListId = GuidParser.ParseNullable(ev.ProportionalElectionMainListId);
    }

    private void Apply(ProportionalElectionCandidateCreated ev)
    {
        var candidate = _mapper.Map<ProportionalElectionCandidate>(ev.ProportionalElectionCandidate);
        var list = FindList(GuidParser.Parse(ev.ProportionalElectionCandidate.ProportionalElectionListId));
        list.Candidates.Add(candidate);
    }

    private void Apply(ProportionalElectionCandidateUpdated ev)
    {
        var candidate = FindCandidate(
            GuidParser.Parse(ev.ProportionalElectionCandidate.ProportionalElectionListId),
            GuidParser.Parse(ev.ProportionalElectionCandidate.Id));

        var removedAccumulation = candidate.Accumulated && !ev.ProportionalElectionCandidate.Accumulated;
        var accumulatedPosition = candidate.AccumulatedPosition;

        _mapper.Map(ev.ProportionalElectionCandidate, candidate);

        if (removedAccumulation)
        {
            var list = Lists.Single(l => l.Candidates.Any(c => c.Id == candidate.Id));
            DecreaseCandidatePositions(list.Candidates, accumulatedPosition);
        }
    }

    private void Apply(ProportionalElectionCandidateAfterTestingPhaseUpdated ev)
    {
        var id = GuidParser.Parse(ev.Id);
        var candidate = Lists.SelectMany(l => l.Candidates).Single(c => c.Id == id);
        _mapper.Map(ev, candidate);
    }

    private void Apply(ProportionalElectionCandidatesReordered ev)
    {
        var list = FindList(GuidParser.Parse(ev.ProportionalElectionListId));

        var grouped = ev.CandidateOrders.Orders
            .GroupBy(o => GuidParser.Parse(o.Id))
            .ToDictionary(x => x.Key, x => x.Select(y => y.Position).OrderBy(y => y).ToList());

        foreach (var candidate in list.Candidates)
        {
            candidate.Position = grouped[candidate.Id][0];
            if (candidate.Accumulated)
            {
                candidate.AccumulatedPosition = grouped[candidate.Id][1];
            }
        }
    }

    private void Apply(ProportionalElectionCandidateDeleted ev)
    {
        var candidateId = GuidParser.Parse(ev.ProportionalElectionCandidateId);
        var list = Lists.Single(l => l.Candidates.Any(c => c.Id == candidateId));
        var existingCandidate = list.Candidates.Single(c => c.Id == candidateId);

        list.Candidates.Remove(existingCandidate);

        DecreaseCandidatePositions(list.Candidates, existingCandidate.Position);
        if (existingCandidate.Accumulated)
        {
            DecreaseCandidatePositions(list.Candidates, existingCandidate.AccumulatedPosition);
        }
    }

    private void Apply(ProportionalElectionMandateAlgorithmUpdated ev)
    {
        MandateAlgorithm = _mapper.Map<ProportionalElectionMandateAlgorithm>(ev.MandateAlgorithm);
    }

    private ProportionalElectionList FindList(Guid listId)
    {
        return Lists.SingleOrDefault(l => l.Id == listId)
            ?? throw new ValidationException($"List {listId} does not exist");
    }

    private ProportionalElectionListUnion FindListUnion(Guid listUnionId)
    {
        return ListUnions.SingleOrDefault(l => l.Id == listUnionId)
            ?? throw new ValidationException($"ListUnion {listUnionId} does not exist");
    }

    private void EnsureUniqueListPosition(ProportionalElectionList changedList)
    {
        var position = changedList.Position;

        if (Lists.Any(l => l.Id != changedList.Id && l.Position == position))
        {
            throw new ValidationException($"List position {position} is already taken.");
        }
    }

    private void EnsureValidListUnion(ProportionalElectionListUnion changedListUnion)
    {
        var position = changedListUnion.Position;
        if (ListUnions.Any(l => l.Id != changedListUnion.Id
                            && l.Position == position
                            && l.ProportionalElectionRootListUnionId == changedListUnion.ProportionalElectionRootListUnionId))
        {
            throw new ValidationException($"ListUnion position {position} is already taken.");
        }

        if (!changedListUnion.ProportionalElectionRootListUnionId.HasValue)
        {
            return;
        }

        var rootListUnion = FindListUnion(changedListUnion.ProportionalElectionRootListUnionId.Value);
        if (rootListUnion.ProportionalElectionRootListUnionId.HasValue)
        {
            throw new ValidationException("A SubListUnion may not contain SubListUnions.");
        }
    }

    private void EnsureListExists(Guid listId)
    {
        if (Lists.All(l => l.Id != listId))
        {
            throw new ValidationException($"List {listId} does not exist");
        }
    }

    private void EnsureListUnionExists(Guid listUnionId)
    {
        if (ListUnions.All(l => l.Id != listUnionId))
        {
            throw new ValidationException($"ListUnion {listUnionId} does not exist");
        }
    }

    private void EnsureCandidateExists(ProportionalElectionList list, Guid candidateId)
    {
        if (list.Candidates.All(c => c.Id != candidateId))
        {
            throw new ValidationException($"Candidate {candidateId} does not exist");
        }
    }

    private void EnsureUniqueCandidatePosition(
        ProportionalElectionList list,
        ProportionalElectionCandidate changedCandidate)
    {
        var position = changedCandidate.Position;
        var accumulatedPosition = changedCandidate.Accumulated
            ? changedCandidate.AccumulatedPosition
            : (int?)null;

        foreach (var candidate in list.Candidates.Where(c => c.Id != changedCandidate.Id))
        {
            if (candidate.Position == position ||
                (candidate.Accumulated && candidate.AccumulatedPosition == position) ||
                (accumulatedPosition != null && accumulatedPosition == candidate.Position) ||
                (accumulatedPosition != null && candidate.Accumulated && accumulatedPosition == candidate.AccumulatedPosition))
            {
                throw new ValidationException($"List position {list.Position} is already taken.");
            }
        }
    }

    private void DecreaseCandidatePositions(IEnumerable<ProportionalElectionCandidate> candidates, int fromPosition)
    {
        foreach (var candidate in candidates.Where(c => c.Position > fromPosition))
        {
            candidate.Position--;
            if (candidate.Accumulated)
            {
                candidate.AccumulatedPosition--;
            }
        }
    }

    private void EnsureListHasSpace(ProportionalElectionList list, ProportionalElectionCandidate candidate)
    {
        var maxCandidates = NumberOfMandates - list.BlankRowCount;
        if (candidate.Position > maxCandidates || (candidate.Accumulated && candidate.AccumulatedPosition > maxCandidates))
        {
            throw new ValidationException($"List is full, cannot have more than {maxCandidates} candidates");
        }
    }

    private void EnsureUniqueCandidateNumber(ProportionalElectionList list, ProportionalElectionCandidate candidate)
    {
        if (list.Candidates.Any(c => c.Id != candidate.Id && c.Number == candidate.Number))
        {
            throw new NonUniqueCandidateNumberException();
        }
    }

    private void EnsureLocalityAndOriginIsSetForNonCommunalDoiType(ProportionalElectionCandidate candidate, CandidateValidationParams candidateValidationParams)
    {
        if (candidateValidationParams.IsLocalityRequired && string.IsNullOrEmpty(candidate.Locality) && !candidateValidationParams.DoiType.IsCommunal())
        {
            throw new ValidationException("Candidate locality is required for non communal political businesses");
        }

        if (candidateValidationParams.IsOriginRequired && string.IsNullOrEmpty(candidate.Origin) && !candidateValidationParams.DoiType.IsCommunal())
        {
            throw new ValidationException("Candidate origin is required for non communal political businesses");
        }
    }

    private void RemoveListUnionsForRemovedList(Guid listId)
    {
        var listUnionIdsToRemove = new HashSet<Guid>();
        foreach (var listUnion in ListUnions)
        {
            listUnion.ProportionalElectionListIds.Remove(listId);

            // remove the list union if this was the main list
            if (listUnion.ProportionalElectionMainListId == listId)
            {
                listUnionIdsToRemove.Add(listUnion.Id);
            }
        }

        foreach (var listUnionId in listUnionIdsToRemove)
        {
            RemoveListUnion(listUnionId);
        }
    }

    private void RemoveListUnion(Guid listUnionId)
    {
        var existingListUnion = ListUnions.Find(x => x.Id == listUnionId);
        if (existingListUnion == null)
        {
            return;
        }

        ListUnions.Remove(existingListUnion);

        foreach (var listUnion in ListUnions.Where(l => l.Position > existingListUnion.Position))
        {
            listUnion.Position--;
        }

        if (existingListUnion.IsSubListUnion)
        {
            return;
        }

        var subListUnions = ListUnions.Where(lu => lu.ProportionalElectionRootListUnionId == existingListUnion.Id).ToList();
        foreach (var subListUnion in subListUnions)
        {
            ListUnions.Remove(subListUnion);
        }
    }

    private void EnsureIsHagenbachBischoffElection()
    {
        if (MandateAlgorithm != ProportionalElectionMandateAlgorithm.HagenbachBischoff)
        {
            throw new ValidationException("The election does not distribute mandates per Hagenbach-Bischoff algorithm");
        }
    }

    private void EnsureListNotInUnion(Guid listId)
    {
        if (ListUnions.Any(l => l.ProportionalElectionListIds.Contains(listId)))
        {
            throw new ProportionalElectionListIsInListUnionException(listId);
        }
    }

    private void EnsureCanSetActive(bool active)
    {
        EnsureNotDeleted();

        if (active)
        {
            EnsureValidListsAndListUnions();
        }
    }

    private void EnsureEVotingNotApproved()
    {
        if (EVotingApproved == true)
        {
            throw new PoliticalBusinessEVotingApprovedException();
        }
    }

    private void EnsureValidListsAndListUnions()
    {
        if (Lists.Count == 0)
        {
            throw new PoliticalBusinessNotCompleteException("A proportional election requires at least one list");
        }

        foreach (var list in Lists)
        {
            if (list.Candidates.Count == 0)
            {
                throw new PoliticalBusinessNotCompleteException("A proportional election list requires at least one candidate");
            }

            if (list.Candidates.Sum(c => !c.Accumulated ? 1 : 2) + list.BlankRowCount != NumberOfMandates)
            {
                throw new PoliticalBusinessNotCompleteException("The count of candidates plus the blank row count of a list must match the number of mandates");
            }
        }

        foreach (var listUnion in ListUnions)
        {
            if (listUnion.ProportionalElectionListIds.Count < 2)
            {
                throw new ProportionalElectionListUnionMissingListsException(Id);
            }
        }
    }
}
