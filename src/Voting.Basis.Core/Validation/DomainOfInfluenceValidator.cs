// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class DomainOfInfluenceValidator : AbstractValidator<DomainOfInfluence>
{
    public DomainOfInfluenceValidator(
        IValidator<DomainOfInfluenceVotingCardPrintData> printDataValidator,
        IValidator<PlausibilisationConfiguration> plausibilisationConfigurationValidator)
    {
        RuleFor(v => v.ReturnAddress).Null().When(v => !v.ResponsibleForVotingCards);
        RuleFor(v => v.ReturnAddress!).NotNull().When(v => v.ResponsibleForVotingCards);
        RuleFor(v => v.PrintData).Null().When(v => !v.ResponsibleForVotingCards);
        RuleFor(v => v.PrintData!).NotNull().SetValidator(printDataValidator).When(v => v.ResponsibleForVotingCards);

        RuleFor(v => v.PlausibilisationConfiguration!).SetValidator(plausibilisationConfigurationValidator);
    }
}
