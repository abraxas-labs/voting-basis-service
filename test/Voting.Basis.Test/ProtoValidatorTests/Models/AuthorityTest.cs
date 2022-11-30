// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class AuthorityTest : ProtoValidatorBaseTest<Authority>
{
    public static Authority NewValid(Action<Authority>? action = null)
    {
        var authority = new Authority
        {
            SecureConnectId = "380590188826699143",
            Name = "Stadt St. Gallen",
            Street = "Musterstrasse 40",
            Zip = "9000",
            City = "St. Gallen",
            Phone = "071 123 12 20",
            Email = "sg-test@abraxas.ch",
        };

        action?.Invoke(authority);
        return authority;
    }

    protected override IEnumerable<Authority> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(18));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(20));
        yield return NewValid(x => x.Name = string.Empty);
        yield return NewValid(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.Street = string.Empty);
        yield return NewValid(x => x.Street = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.Street = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValid(x => x.Zip = string.Empty);
        yield return NewValid(x => x.Zip = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.Zip = RandomStringUtil.GenerateComplexSingleLineText(15));
        yield return NewValid(x => x.City = string.Empty);
        yield return NewValid(x => x.City = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValid(x => x.City = RandomStringUtil.GenerateComplexSingleLineText(50));
        yield return NewValid(x => x.Phone = string.Empty);
        yield return NewValid(x => x.Email = string.Empty);
    }

    protected override IEnumerable<Authority> NotOkMessages()
    {
        yield return NewValid(x => x.SecureConnectId = string.Empty);
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(17));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(21));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateAlphabetic(18));
        yield return NewValid(x => x.Name = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.Name = "Stadt St. Gal\nlen");
        yield return NewValid(x => x.Street = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValid(x => x.Street = "Muster\nstrasse");
        yield return NewValid(x => x.Zip = RandomStringUtil.GenerateComplexSingleLineText(16));
        yield return NewValid(x => x.Zip = "9000\n12");
        yield return NewValid(x => x.City = RandomStringUtil.GenerateComplexSingleLineText(51));
        yield return NewValid(x => x.City = "St. Gal\nlen");
        yield return NewValid(x => x.Phone = "12345");
        yield return NewValid(x => x.Email = "test");
    }
}
