// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Shared.V1;
using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class DomainOfInfluenceVotingCardPrintDataValidator : AbstractValidator<DomainOfInfluenceVotingCardPrintData>
{
    public DomainOfInfluenceVotingCardPrintDataValidator()
    {
        RuleFor(v => v.ShippingAway)
            .Must(v => v is VotingCardShippingFranking.B1 or VotingCardShippingFranking.B2 or VotingCardShippingFranking.A);

        RuleFor(v => v.ShippingReturn)
            .Must(v => v is VotingCardShippingFranking.GasA or VotingCardShippingFranking.GasB or VotingCardShippingFranking.WithoutFranking);
    }
}
