// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class DomainOfInfluenceTest : ProtoValidatorBaseTest<ProtoModels.DomainOfInfluence>
{
    public static ProtoModels.DomainOfInfluence NewValid(Action<ProtoModels.DomainOfInfluence>? action = null)
    {
        var domainOfInfluence = new ProtoModels.DomainOfInfluence
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Name = "Bezirk Uzwil",
            ShortName = "BZ Uz",
            AuthorityName = "Gemeinde Uzwil",
            SecureConnectId = "380590188826699143",
            Type = DomainOfInfluenceType.Bz,
            ParentId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Canton = DomainOfInfluenceCanton.Sg,
            ContactPerson = ContactPersonTest.NewValid(),
            Info = EntityInfoTest.NewValid(),
            ResponsibleForVotingCards = true,
            Bfs = "2442",
            Code = "C2355",
            SortNumber = 3,
            ExportConfigurations = { ExportConfigurationTest.NewValid() },
            ReturnAddress = DomainOfInfluenceVotingCardReturnAddressTest.NewValid(),
            PrintData = DomainOfInfluenceVotingCardPrintDataTest.NewValid(),
            HasLogo = true,
            PlausibilisationConfiguration = PlausibilisationConfigurationTest.NewValid(),
            Parties = { DomainOfInfluencePartyTest.NewValid() },
            ExternalPrintingCenter = true,
            ExternalPrintingCenterEaiMessageType = "1234567",
            NameForProtocol = "Bezirk Uzwil (Protokoll)",
        };

        action?.Invoke(domainOfInfluence);
        return domainOfInfluence;
    }

    protected override IEnumerable<ProtoModels.DomainOfInfluence> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.ShortName = string.Empty);
        yield return NewValid(x => x.ShortName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.ShortName = RandomStringUtil.GenerateSimpleSingleLineText(50));
        yield return NewValid(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(18));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(20));
        yield return NewValid(x => x.ParentId = string.Empty);
        yield return NewValid(x => x.Canton = DomainOfInfluenceCanton.Unspecified);
        yield return NewValid(x => x.ResponsibleForVotingCards = false);
        yield return NewValid(x => x.Bfs = string.Empty);
        yield return NewValid(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(1));
        yield return NewValid(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(8));
        yield return NewValid(x => x.Code = string.Empty);
        yield return NewValid(x => x.Code = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.Code = RandomStringUtil.GenerateSimpleSingleLineText(20));
        yield return NewValid(x => x.SortNumber = 0);
        yield return NewValid(x => x.SortNumber = 1000);
        yield return NewValid(x => x.ExportConfigurations.Clear());
        yield return NewValid(x => x.ReturnAddress = null);
        yield return NewValid(x => x.PrintData = null);
        yield return NewValid(x => x.HasLogo = false);
        yield return NewValid(x => x.Parties.Clear());
        yield return NewValid(x => x.ExternalPrintingCenter = false);
        yield return NewValid(x => x.ExternalPrintingCenterEaiMessageType = string.Empty);
        yield return NewValid(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(7));
        yield return NewValid(x => x.SapCustomerOrderNumber = string.Empty);
        yield return NewValid(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateNumeric(20));
        yield return NewValid(x => x.NameForProtocol = string.Empty);
        yield return NewValid(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValid(x => x.PlausibilisationConfiguration = null);
    }

    protected override IEnumerable<ProtoModels.DomainOfInfluence> NotOkMessages()
    {
        yield return NewValid(x => x.Name = string.Empty);
        yield return NewValid(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.Name = "Bezirk\n Uzwil");
        yield return NewValid(x => x.ShortName = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValid(x => x.ShortName = "BZ\n Uz");
        yield return NewValid(x => x.AuthorityName = string.Empty);
        yield return NewValid(x => x.AuthorityName = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.AuthorityName = "Gemein\nde Uzwil");
        yield return NewValid(x => x.SecureConnectId = string.Empty);
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(17));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(21));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateAlphabetic(18));
        yield return NewValid(x => x.Type = DomainOfInfluenceType.Unspecified);
        yield return NewValid(x => x.Type = (DomainOfInfluenceType)(-1));
        yield return NewValid(x => x.ParentId = "invalid-guid");
        yield return NewValid(x => x.Canton = (DomainOfInfluenceCanton)(-1));
        yield return NewValid(x => x.ContactPerson = null);
        yield return NewValid(x => x.Bfs = RandomStringUtil.GenerateAlphanumericWhitespace(9));
        yield return NewValid(x => x.Bfs = "1234-56");
        yield return NewValid(x => x.Code = RandomStringUtil.GenerateSimpleSingleLineText(21));
        yield return NewValid(x => x.Code = "1234_56");
        yield return NewValid(x => x.SortNumber = -1);
        yield return NewValid(x => x.SortNumber = 1001);
        yield return NewValid(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(6));
        yield return NewValid(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateNumeric(8));
        yield return NewValid(x => x.ExternalPrintingCenterEaiMessageType = RandomStringUtil.GenerateAlphabetic(7));
        yield return NewValid(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateNumeric(21));
        yield return NewValid(x => x.SapCustomerOrderNumber = RandomStringUtil.GenerateAlphabetic(20));
        yield return NewValid(x => x.NameForProtocol = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValid(x => x.NameForProtocol = "Bezirk Uzwil \n(Protokoll)");
    }
}
