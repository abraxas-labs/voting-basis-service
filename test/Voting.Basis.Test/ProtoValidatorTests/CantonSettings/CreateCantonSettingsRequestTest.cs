// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.CantonSettings;

public class CreateCantonSettingsRequestTest : ProtoValidatorBaseTest<CreateCantonSettingsRequest>
{
    protected override IEnumerable<CreateCantonSettingsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(18));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(20));
        yield return NewValidRequest(x => x.ProportionalElectionMandateAlgorithms.Clear());
        yield return NewValidRequest(x => x.MajorityElectionInvalidVotes = false);
        yield return NewValidRequest(x => x.SwissAbroadVotingRightDomainOfInfluenceTypes.Clear());
        yield return NewValidRequest(x => x.EnabledPoliticalBusinessUnionTypes.Clear());
        yield return NewValidRequest(x => x.VotingDocumentsEVotingEaiMessageType = RandomStringUtil.GenerateNumeric(7));
    }

    protected override IEnumerable<CreateCantonSettingsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Canton = DomainOfInfluenceCanton.Unspecified);
        yield return NewValidRequest(x => x.Canton = (DomainOfInfluenceCanton)10);
        yield return NewValidRequest(x => x.AuthorityName = string.Empty);
        yield return NewValidRequest(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValidRequest(x => x.AuthorityName = "St.Ga\nllen");
        yield return NewValidRequest(x => x.SecureConnectId = string.Empty);
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(17));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(21));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateAlphabetic(18));
        yield return NewValidRequest(x => x.ProportionalElectionMandateAlgorithms.Add(ProportionalElectionMandateAlgorithm.Unspecified));
        yield return NewValidRequest(x => x.ProportionalElectionMandateAlgorithms.Add((ProportionalElectionMandateAlgorithm)10));
        yield return NewValidRequest(x => x.MajorityElectionAbsoluteMajorityAlgorithm = CantonMajorityElectionAbsoluteMajorityAlgorithm.Unspecified);
        yield return NewValidRequest(x => x.MajorityElectionAbsoluteMajorityAlgorithm = (CantonMajorityElectionAbsoluteMajorityAlgorithm)10);
        yield return NewValidRequest(x => x.SwissAbroadVotingRight = SwissAbroadVotingRight.Unspecified);
        yield return NewValidRequest(x => x.SwissAbroadVotingRight = (SwissAbroadVotingRight)10);
        yield return NewValidRequest(x => x.SwissAbroadVotingRightDomainOfInfluenceTypes.Add(DomainOfInfluenceType.Unspecified));
        yield return NewValidRequest(x => x.SwissAbroadVotingRightDomainOfInfluenceTypes.Add((DomainOfInfluenceType)(-1)));
        yield return NewValidRequest(x => x.EnabledPoliticalBusinessUnionTypes.Add(PoliticalBusinessUnionType.PoliticalBusinessUnionUnspecified));
        yield return NewValidRequest(x => x.EnabledPoliticalBusinessUnionTypes.Add((PoliticalBusinessUnionType)(-1)));
        yield return NewValidRequest(x => x.VotingDocumentsEVotingEaiMessageType = string.Empty);
        yield return NewValidRequest(x => x.VotingDocumentsEVotingEaiMessageType = RandomStringUtil.GenerateNumeric(6));
        yield return NewValidRequest(x => x.VotingDocumentsEVotingEaiMessageType = RandomStringUtil.GenerateNumeric(8));
        yield return NewValidRequest(x => x.VotingDocumentsEVotingEaiMessageType = RandomStringUtil.GenerateAlphabetic(7));
        yield return NewValidRequest(x => x.ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.Unspecified);
        yield return NewValidRequest(x => x.ProtocolDomainOfInfluenceSortType = (ProtocolDomainOfInfluenceSortType)10);
        yield return NewValidRequest(x => x.ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.Unspecified);
        yield return NewValidRequest(x => x.ProtocolCountingCircleSortType = (ProtocolCountingCircleSortType)10);
    }

    private CreateCantonSettingsRequest NewValidRequest(Action<CreateCantonSettingsRequest>? action = null)
    {
        var request = new CreateCantonSettingsRequest
        {
            Canton = DomainOfInfluenceCanton.Sg,
            AuthorityName = "St.Gallen",
            SecureConnectId = "380590188826699143",
            ProportionalElectionMandateAlgorithms = { ProportionalElectionMandateAlgorithm.HagenbachBischoff, ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum },
            MajorityElectionAbsoluteMajorityAlgorithm = CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
            MajorityElectionInvalidVotes = true,
            SwissAbroadVotingRight = SwissAbroadVotingRight.NoRights,
            SwissAbroadVotingRightDomainOfInfluenceTypes = { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct },
            EnabledPoliticalBusinessUnionTypes = { PoliticalBusinessUnionType.PoliticalBusinessUnionMajorityElection },
            EnabledVotingCardChannels = { CantonSettingsVotingCardChannelTest.NewValid() },
            VotingDocumentsEVotingEaiMessageType = "1234567",
            ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.SortNumber,
            ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.Alphabetical,
        };

        action?.Invoke(request);
        return request;
    }
}
