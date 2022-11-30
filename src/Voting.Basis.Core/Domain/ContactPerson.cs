// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Domain;

public class ContactPerson
{
    public string? FirstName { get; private set; }

    public string? FamilyName { get; private set; }

    public string? Phone { get; private set; }

    public string? MobilePhone { get; private set; }

    public string? Email { get; private set; }
}
