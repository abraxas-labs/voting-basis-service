// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentValidation;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Validation;

public class DateOfBirthValidator : AbstractValidator<DateTime?>
{
    private static readonly DateTime MinDateOfBirth = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DateOfBirthValidator(IClock clock)
    {
        RuleFor(t => t)
            .Must(t => t >= MinDateOfBirth && t < clock.UtcNow)
            .When(t => t.HasValue)
            .WithMessage("Date of birth has to be after 1900-01-01 UTC");
    }
}
