// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.DomainOfInfluence;

public class UpdateDomainOfInfluenceForElectionAdminRequestTest : ProtoValidatorBaseTest<UpdateDomainOfInfluenceForElectionAdminRequest>
{
    protected override IEnumerable<UpdateDomainOfInfluenceForElectionAdminRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ReturnAddress = null);
        yield return NewValidRequest(x => x.PrintData = null);
        yield return NewValidRequest(x => x.Parties.Clear());
        yield return NewValidRequest(x => x.ExternalPrintingCenter = false);
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = string.Empty);
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(7));
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = string.Empty);
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateNumeric(20));
        yield return NewValidRequest(x => x.PlausibilisationConfiguration = null);
    }

    protected override IEnumerable<UpdateDomainOfInfluenceForElectionAdminRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.ContactPerson = null);
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(6));
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(8));
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateAlphabetic(7));
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateNumeric(21));
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateAlphabetic(20));
        yield return NewValidRequest(x => x.VotingCardColor = (VotingCardColor)28);
    }

    private UpdateDomainOfInfluenceForElectionAdminRequest NewValidRequest(Action<UpdateDomainOfInfluenceForElectionAdminRequest>? action = null)
    {
        var request = new UpdateDomainOfInfluenceForElectionAdminRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ContactPerson = ContactPersonTest.NewValid(),
            ReturnAddress = DomainOfInfluenceVotingCardReturnAddressTest.NewValid(),
            PrintData = DomainOfInfluenceVotingCardPrintDataTest.NewValid(),
            PlausibilisationConfiguration = PlausibilisationConfigurationTest.NewValid(),
            Parties = { DomainOfInfluencePartyTest.NewValid() },
            ExternalPrintingCenter = true,
            ExternalPrintingCenterEaiMessageType = "1234567",
        };

        action?.Invoke(request);
        return request;
    }
}
