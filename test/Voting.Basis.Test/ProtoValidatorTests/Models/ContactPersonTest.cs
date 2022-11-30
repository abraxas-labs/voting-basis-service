// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ContactPersonTest : ProtoValidatorBaseTest<ContactPerson>
{
    public static ContactPerson NewValid(Action<ContactPerson>? action = null)
    {
        var contactPerson = new ContactPerson
        {
            FirstName = "Test",
            FamilyName = "Muster",
            Phone = "071 123 12 20",
            MobilePhone = "079 123 12 20",
            Email = "sg-test@abraxas.ch",
        };

        action?.Invoke(contactPerson);
        return contactPerson;
    }

    protected override IEnumerable<ContactPerson> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.FirstName = string.Empty);
        yield return NewValid(x => x.FirstName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.FirstName = RandomStringUtil.GenerateSimpleSingleLineText(50));
        yield return NewValid(x => x.FamilyName = string.Empty);
        yield return NewValid(x => x.FamilyName = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.FamilyName = RandomStringUtil.GenerateSimpleSingleLineText(50));
        yield return NewValid(x => x.Phone = string.Empty);
        yield return NewValid(x => x.MobilePhone = string.Empty);
        yield return NewValid(x => x.Email = string.Empty);
    }

    protected override IEnumerable<ContactPerson> NotOkMessages()
    {
        yield return NewValid(x => x.FirstName = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValid(x => x.FirstName = "Test\ner");
        yield return NewValid(x => x.FamilyName = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValid(x => x.FamilyName = "Must\ner");
        yield return NewValid(x => x.Phone = "12345");
        yield return NewValid(x => x.MobilePhone = "12345");
        yield return NewValid(x => x.Email = "test");
    }
}
