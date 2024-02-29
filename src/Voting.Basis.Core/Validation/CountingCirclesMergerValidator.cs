// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class CountingCirclesMergerValidator : AbstractValidator<CountingCirclesMerger>
{
    public CountingCirclesMergerValidator()
    {
        RuleFor(m => m.MergedCountingCircleIds)
            .Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("Some counting circle ids to merge are duplicates");
        RuleFor(m => m.MergedCountingCircleIds.Count).GreaterThanOrEqualTo(2);
    }
}
