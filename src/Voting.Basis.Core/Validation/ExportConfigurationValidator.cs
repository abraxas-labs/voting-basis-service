// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Data.Models;
using ExportConfiguration = Voting.Basis.Core.Domain.ExportConfiguration;

namespace Voting.Basis.Core.Validation;

public class ExportConfigurationValidator : AbstractValidator<ExportConfiguration>
{
    public ExportConfigurationValidator()
    {
        RuleFor(x => x.SedexConfiguration)
            .Must(x => x is { RecipientIds.Length: > 0 })
            .When(x => x.Provider == ExportProvider.Sedex);

        RuleFor(x => x.SedexConfiguration)
            .Must(x => x == null)
            .When(x => x.Provider != ExportProvider.Sedex);
    }
}
