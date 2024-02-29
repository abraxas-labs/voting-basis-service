// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class MajorityElectionCandidateValidator : AbstractValidator<MajorityElectionCandidate>
{
    public MajorityElectionCandidateValidator(DateOfBirthValidator dateOfBirthValidator)
    {
        RuleFor(c => c.DateOfBirth)
            .SetValidator(dateOfBirthValidator);
    }
}
