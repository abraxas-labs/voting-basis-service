// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Basis.Core.Exceptions;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// A secondary majority election (in german: Nebenwahl).
/// </summary>
public class SecondaryMajorityElection
{
    public SecondaryMajorityElection()
    {
        PoliticalBusinessNumber = string.Empty;
        OfficialDescription = new Dictionary<string, string>();
        ShortDescription = new Dictionary<string, string>();
        Candidates = new List<MajorityElectionCandidate>();
        CandidateReferences = new List<MajorityElectionCandidateReference>();
    }

    public Guid Id { get; internal set; }

    public string PoliticalBusinessNumber { get; private set; }

    public Dictionary<string, string> OfficialDescription { get; private set; }

    public Dictionary<string, string> ShortDescription { get; private set; }

    public int NumberOfMandates { get; private set; }

    public bool Active { get; internal set; }

    public List<MajorityElectionCandidate> Candidates { get; private set; }

    public List<MajorityElectionCandidateReference> CandidateReferences { get; private set; }

    public Guid PrimaryMajorityElectionId { get; set; }

    public bool IndividualCandidatesDisabled { get; private set; }

    public bool IsOnSeparateBallot { get; set; }

    internal MajorityElectionCandidate GetCandidate(Guid candidateId)
    {
        return Candidates.SingleOrDefault(c => c.Id == candidateId)
            ?? throw new ValidationException($"Candidate {candidateId} does not exist");
    }

    internal MajorityElectionCandidateReference GetCandidateReference(Guid referenceId)
    {
        return CandidateReferences.SingleOrDefault(c => c.Id == referenceId)
            ?? throw new ValidationException($"Candidate reference {referenceId} does not exist");
    }

    internal void EnsureValidCandidatePosition(MajorityElectionCandidate changedCandidate, bool creatingCandidate)
    {
        var position = changedCandidate.Position;

        var maxPosition = CandidateReferences.Count + Candidates.Count;
        if (creatingCandidate)
        {
            maxPosition++;
        }

        if (maxPosition < position)
        {
            throw new ValidationException($"Candidate position {position} is not continuous, should at most be {maxPosition}.");
        }

        if (Candidates.Any(c => c.Id != changedCandidate.Id && c.Position == position)
            || CandidateReferences.Any(r => r.Position == position))
        {
            throw new ValidationException($"Candidate position {position} is already taken.");
        }
    }

    internal void EnsureValidCandidatePosition(MajorityElectionCandidateReference candidateReference, bool creatingCandidate)
    {
        var position = candidateReference.Position;

        var maxPosition = CandidateReferences.Count + Candidates.Count;
        if (creatingCandidate)
        {
            maxPosition++;
        }

        if (maxPosition < position)
        {
            throw new ValidationException($"Candidate position {position} is not continuous, should at most be {maxPosition}.");
        }

        if (CandidateReferences.Any(r => r.Id != candidateReference.Id && r.Position == position)
            || Candidates.Any(c => c.Position == position))
        {
            throw new ValidationException($"Candidate position {position} is already taken.");
        }
    }

    internal void EnsureUniqueCandidateNumber(MajorityElectionCandidate candidate, List<MajorityElectionCandidate> referencedCandidates)
    {
        if (Candidates.Any(c => c.Id != candidate.Id && c.Number == candidate.Number)
            || referencedCandidates.Any(r => r.Number == candidate.Number))
        {
            throw new NonUniqueCandidateNumberException();
        }
    }

    internal void EnsureUniqueCandidateNumber(
        MajorityElectionCandidateReference candidateReference,
        List<MajorityElectionCandidate> referencedCandidates,
        string number)
    {
        if (referencedCandidates.Any(r => (r.Id != candidateReference.CandidateId && r.Number == number))
            || Candidates.Any(c => c.Number == number))
        {
            throw new NonUniqueCandidateNumberException();
        }
    }

    internal void RemoveCandidateReferenceForCandidate(Guid candidateId)
    {
        var toDelete = CandidateReferences.Find(cr => cr.CandidateId == candidateId);

        if (toDelete == null)
        {
            return;
        }

        CandidateReferences.Remove(toDelete);
        ReorderCandidatesAfterDeletion(toDelete.Position);
    }

    internal void ReorderCandidatesAfterDeletion(int deletedPosition)
    {
        foreach (var candidate in Candidates.Where(c => c.Position > deletedPosition))
        {
            candidate.Position--;
        }

        foreach (var candidateReference in CandidateReferences.Where(c => c.Position > deletedPosition))
        {
            candidateReference.Position--;
        }
    }
}
