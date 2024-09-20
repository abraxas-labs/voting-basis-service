// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class PoliticalAssemblyValidator : AbstractValidator<PoliticalAssembly>
{
    public PoliticalAssemblyValidator()
    {
        RuleFor(v => v.Description).SetValidator(new TranslationValidator());
    }
}
