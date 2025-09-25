// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class ProportionalElectionCandidateValidator : AbstractValidator<ProportionalElectionCandidate>
{
    public ProportionalElectionCandidateValidator(DateOfBirthValidator dateOfBirthValidator, SwissZipCodeValidator swissZipCodeValidator)
    {
        RuleFor(c => (DateTime?)c.DateOfBirth)
            .SetValidator(dateOfBirthValidator);
        RuleFor(c => c.AccumulatedPosition).GreaterThan(0)
            .Unless(c => !c.Accumulated);
        RuleFor(c => c)
            .Must(c => !c.Accumulated || c.Position != c.AccumulatedPosition);
        RuleFor(c => c.ZipCode)
            .SetValidator(swissZipCodeValidator)
            .When(c => c.Country == "CH");
    }
}
