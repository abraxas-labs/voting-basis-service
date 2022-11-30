// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Lib.Common;

namespace Voting.Basis.Core.Validation;

public class TranslationValidator : AbstractValidator<IDictionary<string, string>>
{
    public TranslationValidator()
    {
        RuleFor(dict => dict.Keys)
            .Must(k => k.OrderBy(x => x).SequenceEqual(Languages.All.OrderBy(x => x)))
            .WithMessage("Not all languages provided");
    }
}
