// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Ech0155_5_1;
using Ech0159_5_1;
using Voting.Basis.Ech.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech.Utils;
using DataModels = Voting.Basis.Data.Models;
using ExtensionType = Ech0155_5_1.ExtensionType;

namespace Voting.Basis.Ech.Mapping.V5;

internal static class VoteMapping
{
    private const string DefaultVoteDescription = "Volksabstimmung vom {0}";
    private const string VoteDescriptionDateFormat = "dd.MM.yyyy";
    private static readonly Dictionary<DataModels.DomainOfInfluenceType, Dictionary<string, string>> VoteDescriptionMapping = new()
    {
        [DataModels.DomainOfInfluenceType.Ch] = new()
        {
            [Languages.German] = "Eidgenössische Volksabstimmung vom {0}",
            [Languages.French] = "Votation populaire fédérale du {0}",
            [Languages.Italian] = "Votazione popolare federale del {0}",
            [Languages.Romansh] = "Votaziun federala dal pievel dals {0}",
        },
        [DataModels.DomainOfInfluenceType.Ct] = Languages.All.ToDictionary(x => x, _ => "Kantonale Volksabstimmung vom {0}"),
        [DataModels.DomainOfInfluenceType.Bz] = Languages.All.ToDictionary(x => x, _ => "Bezirks-Volksabstimmung vom {0}"),
        [DataModels.DomainOfInfluenceType.Mu] = Languages.All.ToDictionary(x => x, _ => "Gemeinde-Volksabstimmung vom {0}"),
        [DataModels.DomainOfInfluenceType.Sk] = Languages.All.ToDictionary(x => x, _ => "Gemeinde-Volksabstimmung vom {0}"),
    };

    internal static (EventInitialDeliveryVoteInformation VoteInformation, DataModels.DomainOfInfluenceType DoiType) ToEchVoteInformation(this IEnumerable<DataModels.Vote> votes, bool eVoting)
    {
        // Ensure consistent ordering
        var orderedVotes = votes.OrderBy(x => x.PoliticalBusinessNumber).ToList();
        var firstVote = orderedVotes[0];

        var voteDescriptionFormats = VoteDescriptionMapping.GetValueOrDefault(firstVote.DomainOfInfluence!.Type, Languages.All.ToDictionary(x => x, _ => DefaultVoteDescription));
        var voteDescriptions = voteDescriptionFormats
            .FilterEchExportLanguages(eVoting)
            .Select(x => new VoteDescriptionInformationTypeVoteDescriptionInfo
            {
                Language = x.Key,
                VoteDescription = string.Format(x.Value, firstVote.Contest.Date.ToString(VoteDescriptionDateFormat)),
            })
            .ToList();

        // Since we do not have a corresponding vote type in our system, just use the first "VOTING vote" ID as the eCH-vote ID
        var voteType = new VoteType
        {
            VoteIdentification = firstVote.Id.ToString(),
            DomainOfInfluence = firstVote.DomainOfInfluence!.ToEchDomainOfInfluence(),
            VoteDescription = voteDescriptions,
        };

        var ballotTypes = orderedVotes
            .SelectMany(v => v.Ballots.OrderBy(b => b.Position))
            .Select((b, i) => b.ToEchBallot(i, eVoting))
            .ToList();
        return (new EventInitialDeliveryVoteInformation
        {
            Vote = voteType,
            ElectronicBallot = ballotTypes,
        }, firstVote.DomainOfInfluence!.Type);
    }

    private static ElectronicBallotType ToEchBallot(this DataModels.Ballot ballot, int positionOffset, bool eVoting)
    {
        // Use the description from the vote instead of the ballot if necessary, since ballot descriptions are optional in VOTING.
        // At least in the cantons SG and TG, they are never filled, since they use a separate vote per ballot.
        var vote = ballot.Vote;
        var officialDescription = (ballot.OfficialDescription.Count > 0
            ? ballot.OfficialDescription
            : vote.OfficialDescription)
            .FilterEchExportLanguages(eVoting);
        var shortDescription = (ballot.ShortDescription.Count > 0
            ? ballot.ShortDescription
            : vote.ShortDescription)
            .FilterEchExportLanguages(eVoting);
        var descriptionInfos = officialDescription
            .Select(d => new ElectronicBallotDescriptionInformationTypeElectronicBallotDescriptionInfo
            {
                Language = d.Key,
                ElectronicBallotDescriptionLong = d.Value,
                ElectronicBallotDescriptionShort = shortDescription.GetValueOrDefault(d.Key),
            })
            .ToList();
        var ballotPosition = ballot.Position + positionOffset;
        var ballotType = new ElectronicBallotType
        {
            ElectronicBallotIdentification = ballot.Id.ToString(),
            ElectronicBallotPosition = ballotPosition.ToString(),
            ElectronicBallotDescription = descriptionInfos,
            ElectronicBallotGroup = null,
            Extension = new ExtensionType
            {
                Any =
                {
                    SerializeToXmlElement(new BallotExtension
                    {
                        VoteId = ballot.VoteId,
                        VoteType = ballot.Vote.Type,
                        VoteOfficicalDescription = vote.OfficialDescription.FilterEchExportLanguages(eVoting).Select(x => new XmlKeyValuePair(x.Key, x.Value)).ToList(),
                        VoteShortDescription = vote.ShortDescription.FilterEchExportLanguages(eVoting).Select(x => new XmlKeyValuePair(x.Key, x.Value)).ToList(),
                        VoteResultAlgorithm = vote.ResultAlgorithm,
                        VoteResultEntry = vote.ResultEntry,
                        VoteReviewProcedure = vote.ReviewProcedure,
                        VoteEnforceResultEntryForCountingCircles = vote.EnforceResultEntryForCountingCircles,
                        BallotSubType = ballot.SubType,
                    }),
                },
            },
        };

        if (ballot.BallotType == DataModels.BallotType.StandardBallot)
        {
            ballotType.StandardElectronicBallot = ballot.ToEchStandardBallot(ballotPosition, eVoting);
        }
        else
        {
            ballotType.VariantElectronicBallot = ballot.ToEchVariantBallot(ballotPosition, eVoting);
        }

        return ballotType;
    }

    private static ElectronicBallotTypeStandardElectronicBallot ToEchStandardBallot(this DataModels.Ballot ballot, int ballotPosition, bool eVoting)
    {
        var question = ballot.BallotQuestions.First();

        var questionInfos = question.Question
            .FilterEchExportLanguages(eVoting)
            .Select(d => new ElectronicBallotQuestionTypeElectronicBallotQuestionInfo
            {
                Language = d.Key,
                ElectronicBallotQuestion = d.Value,
            })
            .ToList();

        return new ElectronicBallotTypeStandardElectronicBallot
        {
            QuestionIdentification = BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, false, question.Number),
            ElectronicBallotQuestionNumber = ballotPosition.ToString(CultureInfo.InvariantCulture), // A standard ballot only includes the ballot position on question number display.
            AnswerInformation = new AnswerInformationType { AnswerType = AnswerTypeType.Item2, },
            ElectronicBallotQuestion = questionInfos,
        };
    }

    private static ElectronicBallotTypeVariantElectronicBallot ToEchVariantBallot(this DataModels.Ballot ballot, int ballotPosition, bool eVoting)
    {
        var questionInformations = new List<ElectronicBallotTypeVariantElectronicBallotQuestionInformation>();

        var questionIdsByNumber = new Dictionary<int, string>();
        var questionNumber = 1;

        foreach (var question in ballot.BallotQuestions.OrderBy(q => q.Number))
        {
            var questionInfos = question.Question
                .FilterEchExportLanguages(eVoting)
                .Select(d => new ElectronicBallotQuestionTypeElectronicBallotQuestionInfo
                {
                    Language = d.Key,
                    ElectronicBallotQuestion = d.Value,
                })
                .ToList();

            var questionId = BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, false, question.Number);
            questionIdsByNumber[question.Number] = questionId;

            var questionInformation = new ElectronicBallotTypeVariantElectronicBallotQuestionInformation
            {
                QuestionIdentification = questionId,
                ElectronicBallotQuestionNumber = ballotPosition.ToString(CultureInfo.InvariantCulture) + ConvertBasisQuestionNumber(questionNumber),
                AnswerInformation = new AnswerInformationType { AnswerType = AnswerTypeType.Item2 },
                ElectronicBallotQuestion = questionInfos,
            };

            questionInformations.Add(questionInformation);

            questionNumber++;
        }

        var tieBreakQuestionInformations = new List<ElectronicBallotTypeVariantElectronicBallotTieBreakInformation>();
        foreach (var tieBreakQuestion in ballot.TieBreakQuestions.OrderBy(t => t.Number))
        {
            var questionInfos = tieBreakQuestion.Question
                .FilterEchExportLanguages(eVoting)
                .Select(d => new TieBreakQuestionTypeTieBreakQuestionInfo
                {
                    Language = d.Key,
                    TieBreakQuestion = d.Value,
                })
                .ToList();

            var tieBreakQuestionInformation = new ElectronicBallotTypeVariantElectronicBallotTieBreakInformation
            {
                AnswerInformation = new AnswerInformationType { AnswerType = AnswerTypeType.Item4 },
                QuestionIdentification = BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, true, tieBreakQuestion.Number),
                TieBreakQuestionNumber = ballotPosition.ToString(CultureInfo.InvariantCulture) + ConvertBasisQuestionNumber(questionNumber),
                TieBreakQuestion = questionInfos,
                ReferencedQuestion1 = questionIdsByNumber[tieBreakQuestion.Question1Number],
                ReferencedQuestion2 = questionIdsByNumber[tieBreakQuestion.Question2Number],
            };

            tieBreakQuestionInformations.Add(tieBreakQuestionInformation);

            questionNumber++;
        }

        return new ElectronicBallotTypeVariantElectronicBallot
        {
            QuestionInformation = questionInformations,
            TieBreakInformation = tieBreakQuestionInformations,
        };
    }

    private static char ConvertBasisQuestionNumber(int number)
    {
        if (number < 1 || number > 26)
        {
            throw new ValidationException($"Cannot convert the question number '{number}' to an eCH number");
        }

        // 1 = a, 2 = b, ...
        return (char)(number + 96);
    }

    private static XmlElement SerializeToXmlElement<T>(T value)
    {
        var doc = new XmlDocument();

        using (var writer = doc.CreateNavigator()!.AppendChild())
        {
            new XmlSerializer(typeof(T)).Serialize(writer, value);
        }

        return doc.DocumentElement!;
    }
}
