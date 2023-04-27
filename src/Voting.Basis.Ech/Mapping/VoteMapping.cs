// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using eCH_0155_4_0;
using eCH_0159_4_0;
using Voting.Lib.Common;
using Voting.Lib.Ech.Utils;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class VoteMapping
{
    private const string DefaultVoteDescription = "Volksabstimmung vom {0}";
    private const string VoteDescriptionDateFormat = "dd.MM.yyyy";
    private static readonly Dictionary<DataModels.DomainOfInfluenceType, string> VoteDescriptionMapping = new()
    {
        [DataModels.DomainOfInfluenceType.Ch] = "Eidgenössische Volksabstimmung vom {0}",
        [DataModels.DomainOfInfluenceType.Ct] = "Kantonale Volksabstimmung vom {0}",
        [DataModels.DomainOfInfluenceType.Bz] = "Bezirks-Volksabstimmung vom {0}",
        [DataModels.DomainOfInfluenceType.Mu] = "Gemeinde-Volksabstimmung vom {0}",
        [DataModels.DomainOfInfluenceType.Sk] = "Gemeinde-Volksabstimmung vom {0}",
    };

    internal static VoteInformation ToEchVoteInformation(this IEnumerable<DataModels.Vote> votes)
    {
        // Ensure consistent ordering
        var orderedVotes = votes.OrderBy(x => x.PoliticalBusinessNumber).ToList();
        var firstVote = orderedVotes[0];

        var voteDescriptionFormat = VoteDescriptionMapping.GetValueOrDefault(firstVote.DomainOfInfluence!.Type, DefaultVoteDescription);
        var voteDescription = string.Format(voteDescriptionFormat, firstVote.Contest.Date.ToString(VoteDescriptionDateFormat));
        var voteDescriptions = Languages.All
            .Select(x => VoteDescriptionInfoType.Create(x, voteDescription))
            .ToList();

        // Since we do not have a corresponding vote type in our system, just use the first "VOTING vote" ID as the eCH-vote ID
        var voteType = VoteType.Create(
            firstVote.Id.ToString(),
            firstVote.DomainOfInfluenceId.ToString(),
            VoteDescriptionInformationType.Create(voteDescriptions));

        var questionNumber = 1;
        var ballotTypes = orderedVotes
            .SelectMany(v => v.Ballots.OrderBy(b => b.Position))
            .Select((b, i) => b.ToEchBallot(i, ref questionNumber))
            .ToArray();
        return VoteInformation.Create(voteType, ballotTypes);
    }

    internal static IEnumerable<DataModels.Vote> ToBasisVotes(this VoteInformation vote, IdLookup idLookup)
    {
        // eCH votes correspond to VOTING ballots
        for (var i = 0; i < vote.Ballot.Length; i++)
        {
            var ballot = vote.Ballot[i];

            var voteId = Guid.NewGuid();
            var descriptionInfos = ballot
                .BallotDescription
                ?.BallotDescriptionInfo;
            var longDescription = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.BallotDescriptionLong, ballot.BallotIdentification);
            var shortDescription = descriptionInfos.ToLanguageDictionary(x => x.Language, x => x.BallotDescriptionShort, ballot.BallotIdentification);
            var basisBallot = ballot.ToBasisBallot(voteId, idLookup, i);

            yield return new DataModels.Vote
            {
                Id = voteId,
                OfficialDescription = longDescription,
                ShortDescription = shortDescription,
                Ballots = new[] { basisBallot },
                ResultEntry = DataModels.VoteResultEntry.FinalResults,
                ResultAlgorithm = DataModels.VoteResultAlgorithm.PopularMajority,
                ReviewProcedure = DataModels.VoteReviewProcedure.Electronically,

                // see https://jira.abraxas-tools.ch/jira/browse/VOTING-1169?focusedCommentId=640226&page=com.atlassian.jira.plugin.system.issuetabpanels:comment-tabpanel#comment-640226
                EnforceResultEntryForCountingCircles = basisBallot.BallotType == DataModels.BallotType.StandardBallot,
            };
        }
    }

    private static Ballot ToEchBallot(this DataModels.Ballot ballot, int positionOffset, ref int questionNumber)
    {
        // Use the description from the vote instead of the ballot, since ballot descriptions are optional in VOTING.
        // At least in the cantons SG and TG, they are never filled, since they use a separate vote per ballot.
        var vote = ballot.Vote;
        var descriptionInfos = vote.OfficialDescription
            .Select(d => BallotDescriptionInfo.Create(d.Key, d.Value, vote.ShortDescription.GetValueOrDefault(d.Key)))
            .ToList();

        var ballotDescription = BallotDescriptionInformation.Create(descriptionInfos);

        var ballotTypeChoice = ballot.BallotType == DataModels.BallotType.StandardBallot
            ? (object)ballot.ToEchStandardBallot(ref questionNumber)
            : ballot.ToEchVariantBallot(ref questionNumber);

        return Ballot.Create(
            ballot.Id.ToString(),
            ballot.Position + positionOffset,
            ballotDescription,
            null,
            ballotTypeChoice,
            null);
    }

    private static StandardBallotType ToEchStandardBallot(this DataModels.Ballot ballot, ref int questionNumber)
    {
        var question = ballot.BallotQuestions.First();

        var questionInfos = question.Question
            .Select(d => BallotQuestionInfo.Create(d.Key, d.Value))
            .ToList();
        var questionType = BallotQuestion.Create(questionInfos);

        var questionNumberAsString = questionNumber.ToString(CultureInfo.InvariantCulture);
        questionNumber++;

        return StandardBallotType.Create(
            BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, false, question.Number),
            questionNumberAsString,
            AnswerInformationType.Create(AnswerType.YesNoEmpty),
            questionType);
    }

    private static VariantBallot ToEchVariantBallot(this DataModels.Ballot ballot, ref int questionNumber)
    {
        var questionInformations = new List<QuestionInformationType>();

        var questionIdsByNumber = new Dictionary<int, string>();
        foreach (var question in ballot.BallotQuestions.OrderBy(q => q.Number))
        {
            var questionInfos = question.Question
                .Select(d => BallotQuestionInfo.Create(d.Key, d.Value))
                .ToList();
            var questionType = BallotQuestion.Create(questionInfos);

            var questionId = BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, false, question.Number);
            questionIdsByNumber[question.Number] = questionId;

            var questionInformation = QuestionInformationType.Create(
                questionId,
                questionNumber.ToString(CultureInfo.InvariantCulture),
                (uint?)questionNumber,
                AnswerType.YesNoEmpty,
                questionType);
            questionInformations.Add(questionInformation);

            questionNumber++;
        }

        var tieBreakQuestionInformations = new List<TieBreakInformationType>();
        foreach (var tieBreakQuestion in ballot.TieBreakQuestions.OrderBy(t => t.Number))
        {
            var questionInfos = tieBreakQuestion.Question
                .Select(d => TieBreakQuestionInfo.Create(d.Key, d.Value))
                .ToList();
            var tieBreakQuestionType = TieBreakQuestion.Create(questionInfos);

            var tieBreakQuestionInformation = TieBreakInformationType.Create(
                AnswerType.InitiativeCounterdraft,
                BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, true, tieBreakQuestion.Number),
                questionNumber.ToString(CultureInfo.InvariantCulture),
                (uint?)questionNumber,
                tieBreakQuestionType,
                questionIdsByNumber[tieBreakQuestion.Question1Number],
                questionIdsByNumber[tieBreakQuestion.Question2Number]);
            tieBreakQuestionInformations.Add(tieBreakQuestionInformation);

            questionNumber++;
        }

        return VariantBallot.Create(questionInformations, tieBreakQuestionInformations);
    }

    private static DataModels.Ballot ToBasisBallot(this Ballot ballot, Guid voteId, IdLookup idLookup, int positionOffset)
    {
        var ballotId = idLookup.GuidForId(ballot.BallotIdentification);
        var ballotType = DataModels.BallotType.StandardBallot;
        var questions = new List<DataModels.BallotQuestion>();
        var tieBreakQuestions = new List<DataModels.TieBreakQuestion>();

        if (ballot.BallotTypeChoice is StandardBallotType standardBallot)
        {
            questions.Add(standardBallot.ToBasisQuestion(ballotId, idLookup));
        }
        else if (ballot.BallotTypeChoice is VariantBallot variantBallot)
        {
            ballotType = DataModels.BallotType.VariantsBallot;
            questions.AddRange(variantBallot.QuestionInformation.Select((x, i) => x.ToBasisQuestion(ballotId, idLookup, i)));

            if (variantBallot.TieBreakInformation?.Count > 0)
            {
                tieBreakQuestions.AddRange(variantBallot.TieBreakInformation.Select((x, i) => x.ToBasisTieBreakQuestion(ballotId, idLookup, i, questions)));
            }
        }

        return new DataModels.Ballot
        {
            Id = ballotId,
            VoteId = voteId,
            Position = ballot.BallotPosition - positionOffset,
            BallotType = ballotType,
            BallotQuestions = questions,
            TieBreakQuestions = tieBreakQuestions,
            HasTieBreakQuestions = tieBreakQuestions.Count > 0,
        };
    }

    private static DataModels.BallotQuestion ToBasisQuestion(this StandardBallotType ballot, Guid ballotId, IdLookup idLookup)
    {
        var questionInfos = ballot
            .BallotQuestion
            ?.BallotQuestionInfo;
        var question = questionInfos.ToLanguageDictionary(x => x.Language, x => x.BallotQuestion, ballot.QuestionIdentification);

        return new DataModels.BallotQuestion
        {
            Id = idLookup.GuidForId(ballot.QuestionIdentification),
            BallotId = ballotId,
            Number = 1,
            Question = question,
        };
    }

    private static DataModels.BallotQuestion ToBasisQuestion(this QuestionInformationType questionType, Guid ballotId, IdLookup idLookup, int position)
    {
        var questionInfos = questionType
            .BallotQuestion
            .BallotQuestionInfo;
        var question = questionInfos.ToLanguageDictionary(x => x.Language, x => x.BallotQuestion, questionType.QuestionIdentification);

        return new DataModels.BallotQuestion
        {
            Id = idLookup.GuidForId(questionType.QuestionIdentification),
            BallotId = ballotId,
            Number = position + 1,
            Question = question,
        };
    }

    private static DataModels.TieBreakQuestion ToBasisTieBreakQuestion(
        this TieBreakInformationType tieBreak,
        Guid ballotId,
        IdLookup idLookup,
        int position,
        List<DataModels.BallotQuestion> ballotQuestions)
    {
        var questionInfos = tieBreak
            .TieBreakQuestion
            ?.TieBreakQuestionInfo;
        var question = questionInfos.ToLanguageDictionary(x => x.Language, x => x.TieBreakQuestion, tieBreak.TieBreakQuestionNumber);

        return new DataModels.TieBreakQuestion
        {
            Id = idLookup.GuidForId(tieBreak.QuestionIdentification),
            Number = position + 1,
            Question = question,
            BallotId = ballotId,
            Question1Number = FindQuestionNumber(tieBreak.ReferencedQuestion1, idLookup, ballotQuestions),
            Question2Number = FindQuestionNumber(tieBreak.ReferencedQuestion2, idLookup, ballotQuestions),
        };
    }

    private static int FindQuestionNumber(string questionId, IdLookup idLookup, List<DataModels.BallotQuestion> ballotQuestions)
    {
        var mappedQuestionId = idLookup.GuidForId(questionId);
        var question = ballotQuestions.First(q => q.Id == mappedQuestionId);
        return question.Number;
    }
}
