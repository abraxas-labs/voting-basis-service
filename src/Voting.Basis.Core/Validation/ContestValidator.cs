// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class ContestValidator : AbstractValidator<Contest>
{
    public ContestValidator()
    {
        RuleFor(v => v.Description).SetValidator(new TranslationValidator());
        RuleFor(v => v.EVotingFrom).NotNull()
            .Unless(x => !x.EVoting);
        RuleFor(v => v.EVotingTo).NotNull()
            .Unless(x => !x.EVoting);
    }
}
