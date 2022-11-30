// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class EntityOrdersValidator : AbstractValidator<IEnumerable<EntityOrder>>
{
    public EntityOrdersValidator()
    {
        RuleFor(eo => eo)
            .Must(OnlyContainsPositionsWithoutGaps);
    }

    private bool OnlyContainsPositionsWithoutGaps(IEnumerable<EntityOrder> orders)
    {
        // validate all positions are 1 indexed without any gap
        return !orders
            .OrderBy(o => o.Position)
            .Where((t, i) => t.Position != i + 1)
            .Any();
    }
}
