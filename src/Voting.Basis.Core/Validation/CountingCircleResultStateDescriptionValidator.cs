// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Data.Models;
using CountingCircleResultStateDescription = Voting.Basis.Core.Domain.CountingCircleResultStateDescription;

namespace Voting.Basis.Core.Validation;

public class CountingCircleResultStateDescriptionValidator : AbstractValidator<CountingCircleResultStateDescription>
{
    public CountingCircleResultStateDescriptionValidator()
    {
        RuleFor(x => x.State)
            .IsInEnum()
            .NotEqual(CountingCircleResultState.Initial) // needs no description
            .NotEqual(CountingCircleResultState.Unspecified);

        RuleFor(x => x.Description)
            .NotEmpty();
    }
}
