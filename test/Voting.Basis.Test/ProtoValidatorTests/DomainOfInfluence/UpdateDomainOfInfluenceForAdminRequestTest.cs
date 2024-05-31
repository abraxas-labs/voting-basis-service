// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.DomainOfInfluence;

public class UpdateDomainOfInfluenceForAdminRequestTest : ProtoValidatorBaseTest<UpdateDomainOfInfluenceForAdminRequest>
{
    protected override IEnumerable<UpdateDomainOfInfluenceForAdminRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValidRequest(x => x.ShortName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.ShortName = RandomStringUtil.GenerateSimpleSingleLineText(50));
        yield return NewValidRequest(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValidRequest(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(18));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(20));
        yield return NewValidRequest(x => x.Canton = DomainOfInfluenceCanton.Unspecified);
        yield return NewValidRequest(x => x.ResponsibleForVotingCards = false);
        yield return NewValidRequest(x => x.Bfs = string.Empty);
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(8));
        yield return NewValidRequest(x => x.Code = string.Empty);
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateAlphanumericWhitespace(12));
        yield return NewValidRequest(x => x.SortNumber = 0);
        yield return NewValidRequest(x => x.SortNumber = 1000);
        yield return NewValidRequest(x => x.ExportConfigurations.Clear());
        yield return NewValidRequest(x => x.ReturnAddress = null);
        yield return NewValidRequest(x => x.PrintData = null);
        yield return NewValidRequest(x => x.Parties.Clear());
        yield return NewValidRequest(x => x.ExternalPrintingCenter = false);
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = string.Empty);
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(7));
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = string.Empty);
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateNumeric(20));
        yield return NewValidRequest(x => x.NameForProtocol = string.Empty);
        yield return NewValidRequest(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(100));
    }

    protected override IEnumerable<UpdateDomainOfInfluenceForAdminRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.Name = string.Empty);
        yield return NewValidRequest(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValidRequest(x => x.Name = "Bezirk\n Uzwil");
        yield return NewValidRequest(x => x.ShortName = string.Empty);
        yield return NewValidRequest(x => x.ShortName = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValidRequest(x => x.ShortName = "BZ\n Uz");
        yield return NewValidRequest(x => x.AuthorityName = string.Empty);
        yield return NewValidRequest(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValidRequest(x => x.AuthorityName = "Gemein\nde Uzwil");
        yield return NewValidRequest(x => x.SecureConnectId = string.Empty);
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(17));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(21));
        yield return NewValidRequest(x => x.SecureConnectId = RandomStringUtil.GenerateAlphabetic(18));
        yield return NewValidRequest(x => x.Type = DomainOfInfluenceType.Unspecified);
        yield return NewValidRequest(x => x.Type = (DomainOfInfluenceType)(-1));
        yield return NewValidRequest(x => x.Canton = (DomainOfInfluenceCanton)(-1));
        yield return NewValidRequest(x => x.ContactPerson = null);
        yield return NewValidRequest(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(9));
        yield return NewValidRequest(x => x.Bfs = "1234-56");
        yield return NewValidRequest(x => x.Code = RandomStringUtil.GenerateAlphanumericWhitespace(13));
        yield return NewValidRequest(x => x.Code = "1234-56");
        yield return NewValidRequest(x => x.SortNumber = -1);
        yield return NewValidRequest(x => x.SortNumber = 1001);
        yield return NewValidRequest(x => x.PlausibilisationConfiguration = null);
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(6));
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(8));
        yield return NewValidRequest(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateAlphabetic(7));
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateNumeric(21));
        yield return NewValidRequest(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateAlphabetic(20));
        yield return NewValidRequest(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.NameForProtocol = "Bezirk Uzwil \n(Protokoll)");
        yield return NewValidRequest(x => x.VotingCardColor = (VotingCardColor)28);
    }

    private UpdateDomainOfInfluenceForAdminRequest NewValidRequest(Action<UpdateDomainOfInfluenceForAdminRequest>? action = null)
    {
        var request = new UpdateDomainOfInfluenceForAdminRequest
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Name = "Bezirk Uzwil",
            ShortName = "BZ Uz",
            AuthorityName = "Gemeinde Uzwil",
            SecureConnectId = "380590188826699143",
            Type = DomainOfInfluenceType.Bz,
            Canton = DomainOfInfluenceCanton.Sg,
            ContactPerson = ContactPersonTest.NewValid(),
            ResponsibleForVotingCards = true,
            Bfs = "2442",
            Code = "C2355",
            SortNumber = 3,
            ExportConfigurations = { ExportConfigurationTest.NewValid() },
            ReturnAddress = DomainOfInfluenceVotingCardReturnAddressTest.NewValid(),
            PrintData = DomainOfInfluenceVotingCardPrintDataTest.NewValid(),
            PlausibilisationConfiguration = PlausibilisationConfigurationTest.NewValid(),
            Parties = { DomainOfInfluencePartyTest.NewValid() },
            ExternalPrintingCenter = true,
            ExternalPrintingCenterEaiMessageType = "1234567",
            NameForProtocol = "Bezirk Uzwil (Protokoll)",
        };

        action?.Invoke(request);
        return request;
    }
}
