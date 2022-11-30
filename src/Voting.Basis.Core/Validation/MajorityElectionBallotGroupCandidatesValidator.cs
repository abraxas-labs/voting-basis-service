// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class MajorityElectionBallotGroupCandidatesValidator : AbstractValidator<MajorityElectionBallotGroupCandidates>
{
    public MajorityElectionBallotGroupCandidatesValidator()
    {
        RuleFor(e => e.EntryCandidates)
            .Must(c => c.Count > 0)
            .WithMessage("At least one candidate must be provided.");
        RuleFor(e => e.EntryCandidates)
            .Must(HaveNoDuplicateEntries).WithMessage("{PropertyName} has duplicate entries.");
        RuleForEach(e => e.EntryCandidates)
            .Must(HaveNoDuplicateCandidates).WithMessage("{PropertyName} has duplicate entry candidates.");
    }

    private bool HaveNoDuplicateEntries(MajorityElectionBallotGroupCandidates candidates, IReadOnlyCollection<MajorityElectionBallotGroupEntryCandidates> entryCandidates)
    {
        return entryCandidates.Select(e => e.BallotGroupEntryId).Distinct().Count() == entryCandidates.Count;
    }

    private bool HaveNoDuplicateCandidates(MajorityElectionBallotGroupEntryCandidates entry)
    {
        return entry.CandidateIds.Distinct().Count() == entry.CandidateIds.Count;
    }
}
