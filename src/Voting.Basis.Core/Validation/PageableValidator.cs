// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Core.Validation;

public class PageableValidator : AbstractValidator<Pageable>
{
    private const int MaxPageSize = 100;

    public PageableValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0);
        RuleFor(x => x.PageSize).LessThanOrEqualTo(MaxPageSize);
    }
}
