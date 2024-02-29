// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Domain;

public class DomainOfInfluenceVotingCardReturnAddress
{
    public string AddressLine1 { get; private set; } = string.Empty;

    public string AddressLine2 { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string AddressAddition { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;
}
