// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using DomainOfInfluence = Voting.Basis.Core.Domain.DomainOfInfluence;
using DomainOfInfluenceVotingCardPrintData = Voting.Basis.Core.Domain.DomainOfInfluenceVotingCardPrintData;
using ExportConfiguration = Voting.Basis.Core.Domain.ExportConfiguration;
using PlausibilisationConfiguration = Voting.Basis.Core.Domain.PlausibilisationConfiguration;

namespace Voting.Basis.Core.Validation;

public class DomainOfInfluenceValidator : AbstractValidator<DomainOfInfluence>
{
    public DomainOfInfluenceValidator(
        IValidator<DomainOfInfluenceVotingCardPrintData> printDataValidator,
        IValidator<PlausibilisationConfiguration> plausibilisationConfigurationValidator,
        IValidator<ExportConfiguration> exportConfigurationValidator)
    {
        RuleFor(v => v.ReturnAddress).Null().When(v => !v.ResponsibleForVotingCards);
        RuleFor(v => v.ReturnAddress!).NotNull().When(v => v.ResponsibleForVotingCards);
        RuleFor(v => v.PrintData).Null().When(v => !v.ResponsibleForVotingCards);
        RuleFor(v => v.PrintData!).NotNull().SetValidator(printDataValidator).When(v => v.ResponsibleForVotingCards);
        RuleFor(v => v.SwissPostData).Null().When(v => !v.ResponsibleForVotingCards);
        RuleFor(v => v.SwissPostData!).NotNull().When(v => v.ResponsibleForVotingCards);

        RuleFor(v => v.PlausibilisationConfiguration!).SetValidator(plausibilisationConfigurationValidator);
        RuleForEach(v => v.ExportConfigurations).SetValidator(exportConfigurationValidator);
    }
}
