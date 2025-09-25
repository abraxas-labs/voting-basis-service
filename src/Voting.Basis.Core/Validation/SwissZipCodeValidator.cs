// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;

namespace Voting.Basis.Core.Validation;

public class SwissZipCodeValidator : AbstractValidator<string?>
{
    public SwissZipCodeValidator()
    {
        RuleFor(x => x)
            .Must(x => int.TryParse(x, out var zipCode) && zipCode is >= 1000 and <= 9999)
            .When(x => !string.IsNullOrEmpty(x))
            .WithMessage(x => $"Zip code {x} is not a valid swiss zip code.");
    }
}
