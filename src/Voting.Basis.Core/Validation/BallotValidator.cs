// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Ballot = Voting.Basis.Core.Domain.Ballot;
using BallotQuestion = Voting.Basis.Core.Domain.BallotQuestion;
using TieBreakQuestion = Voting.Basis.Core.Domain.TieBreakQuestion;

namespace Voting.Basis.Core.Validation;

public class BallotValidator : AbstractValidator<Ballot>
{
    private const int BinomialCoefficientPairsK = 2;

    public BallotValidator(PublisherConfig config)
    {
        RuleFor(v => v.BallotQuestions)
            .Must(q => ContainsQuestionNumberOnlyOnceWithNoGaps(q.Select(qq => qq.Number))).WithMessage("Numbers of the {PropertyName} have gaps.");

        When(x => x.BallotType == BallotType.StandardBallot, () =>
        {
            RuleFor(b => b.BallotQuestions).Must(b => b.Count == 1 && b[0].Number == 1 && b[0].Type == BallotQuestionType.MainBallot).WithMessage($"A standard ballot must have exactly one question with the number 1 and type {BallotQuestionType.MainBallot}.");
            RuleFor(b => b.HasTieBreakQuestions).Must(x => !x).WithMessage("A standard ballot cannot have tie break questions.");
            RuleFor(b => b.TieBreakQuestions).Must(x => x.Count == 0).WithMessage("A standard ballot cannot have tie break questions.");
        });

        When(x => x.BallotType == BallotType.VariantsBallot, () =>
        {
            RuleFor(b => b.BallotQuestions)
                .Must(b => b.Count > 1).WithMessage("A variant ballot must have more than one question.")
                .Must(b => b.All(TypeMatchQuestionNumber))
                .Must(b => b.Count <= config.Vote.MaxVariantBallotQuestionCount).WithMessage($"A variant ballot can have {config.Vote.MaxVariantBallotQuestionCount} questions at max.")
                .Must(b => ContainsQuestionNumberOnlyOnceWithNoGaps(b.Select(bb => bb.Number))).WithMessage("Numbers of the {PropertyName} have gaps.");
            RuleFor(b => b.TieBreakQuestions)
                .Must(b => ContainsQuestionNumberOnlyOnceWithNoGaps(b.Select(bb => bb.Number))).WithMessage("Numbers of the {PropertyName} have gaps.")
                .Must(HasATieBreakQuestionForEachQuestionPair).WithMessage("{PropertyName} does not have the correct count.")
                .When(x => x.HasTieBreakQuestions);
        });

        When(x => x.SubType == BallotSubType.Unspecified, () =>
        {
            RuleFor(b => b.ShortDescription)
                .Must(b => b.Count == 0).WithMessage("A normal ballot does not need a short description.");
            RuleFor(b => b.OfficialDescription)
                .Must(b => b.Count == 0).WithMessage("A normal ballot does not need an official description.");
        });

        When(x => x.SubType != BallotSubType.Unspecified, () =>
        {
            RuleFor(v => v.ShortDescription).SetValidator(new TranslationValidator());
            RuleFor(v => v.OfficialDescription).SetValidator(new TranslationValidator());
            RuleFor(b => b.BallotQuestions)
                .Must(b => b.Count == 1).WithMessage("A ballot with a sub type must have exactly one question");
            RuleFor(b => b.BallotQuestions)
                .Must(b => b.All(x => x.Type == BallotQuestionType.MainBallot)).WithMessage("A ballot with a sub type must have a main ballot question");
            RuleFor(b => b.TieBreakQuestions)
                .Must(b => b.Count == 0).WithMessage("A ballot with a sub type cannot have a tie break question");
        });
    }

    private bool HasATieBreakQuestionForEachQuestionPair(
        Ballot ballot,
        IEnumerable<TieBreakQuestion> tieBreakQuestions)
    {
        var pairs = new HashSet<(int, int)>();
        foreach (var tieBreakQuestion in tieBreakQuestions)
        {
            // doesn't matter which is question1 and which question2
            if (!pairs.Add((tieBreakQuestion.Question1Number, tieBreakQuestion.Question2Number)))
            {
                return false;
            }

            if (!pairs.Add((tieBreakQuestion.Question2Number, tieBreakQuestion.Question1Number)))
            {
                return false;
            }
        }

        var expectedCount = 2 * MathUtils.BinomialCoefficient(ballot.BallotQuestions.Count, BinomialCoefficientPairsK);
        return pairs.Count == expectedCount;
    }

    private bool ContainsQuestionNumberOnlyOnceWithNoGaps(IEnumerable<int> questionNumbers)
    {
        return !questionNumbers.OrderBy(x => x)
            .Where((t, i) => t != i + 1) // number should always be the index + 1
            .Any();
    }

    private bool TypeMatchQuestionNumber(BallotQuestion ballotQuestion)
    {
        return ballotQuestion.Number switch
        {
            1 => ballotQuestion.Type is BallotQuestionType.MainBallot,
            > 1 => ballotQuestion.Type is BallotQuestionType.CounterProposal or BallotQuestionType.Variant,
            _ => false,
        };
    }
}
