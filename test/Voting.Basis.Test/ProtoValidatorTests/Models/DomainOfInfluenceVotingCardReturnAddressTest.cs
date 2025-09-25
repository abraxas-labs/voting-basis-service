// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class DomainOfInfluenceVotingCardReturnAddressTest : ProtoValidatorBaseTest<DomainOfInfluenceVotingCardReturnAddress>
{
    public static DomainOfInfluenceVotingCardReturnAddress NewValid(Action<DomainOfInfluenceVotingCardReturnAddress>? action = null)
    {
        var domainOfInfluenceVotingCardReturnAddress = new DomainOfInfluenceVotingCardReturnAddress
        {
            AddressLine1 = "Stadtverwaltung Gossau",
            AddressLine2 = "Postfach 12",
            Street = "Bahnhofstrasse 25",
            AddressAddition = "Haupteingang",
            ZipCode = "9200",
            City = "Gossau",
            Country = "Schweiz",
        };

        action?.Invoke(domainOfInfluenceVotingCardReturnAddress);
        return domainOfInfluenceVotingCardReturnAddress;
    }

    protected override IEnumerable<DomainOfInfluenceVotingCardReturnAddress> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.AddressLine1 = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.AddressLine1 = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValid(x => x.AddressLine2 = string.Empty);
        yield return NewValid(x => x.AddressLine2 = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.AddressLine2 = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValid(x => x.Street = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.Street = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValid(x => x.AddressAddition = string.Empty);
        yield return NewValid(x => x.AddressAddition = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.AddressAddition = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValid(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(15));
        yield return NewValid(x => x.City = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.City = RandomStringUtil.GenerateComplexSingleLineText(50));
        yield return NewValid(x => x.Country = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.Country = RandomStringUtil.GenerateSimpleSingleLineText(50));
    }

    protected override IEnumerable<DomainOfInfluenceVotingCardReturnAddress> NotOkMessages()
    {
        yield return NewValid(x => x.AddressLine1 = string.Empty);
        yield return NewValid(x => x.AddressLine1 = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValid(x => x.AddressLine1 = "Stadt\nverwaltung Gossau");
        yield return NewValid(x => x.AddressLine2 = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValid(x => x.AddressLine2 = "Post\nfach 12");
        yield return NewValid(x => x.Street = string.Empty);
        yield return NewValid(x => x.Street = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValid(x => x.Street = "Bahnhof\nstrasse 25");
        yield return NewValid(x => x.AddressAddition = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValid(x => x.AddressAddition = "Haupt\neingang");
        yield return NewValid(x => x.ZipCode = string.Empty);
        yield return NewValid(x => x.ZipCode = RandomStringUtil.GenerateComplexSingleLineText(16));
        yield return NewValid(x => x.ZipCode = "9000\n12");
        yield return NewValid(x => x.City = string.Empty);
        yield return NewValid(x => x.City = RandomStringUtil.GenerateComplexSingleLineText(51));
        yield return NewValid(x => x.City = "Gos\nsau");
        yield return NewValid(x => x.Country = string.Empty);
        yield return NewValid(x => x.Country = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValid(x => x.Country = "Gos\nsau");
    }
}
