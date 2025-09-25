// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class MajorityElectionCandidateValidator : AbstractValidator<MajorityElectionCandidate>
{
    public MajorityElectionCandidateValidator(DateOfBirthValidator dateOfBirthValidator, SwissZipCodeValidator swissZipCodeValidator)
    {
        RuleFor(c => c.DateOfBirth)
            .SetValidator(dateOfBirthValidator);
        RuleFor(c => c.ZipCode)
            .SetValidator(swissZipCodeValidator)
            .When(c => c.Country == "CH");
    }
}
