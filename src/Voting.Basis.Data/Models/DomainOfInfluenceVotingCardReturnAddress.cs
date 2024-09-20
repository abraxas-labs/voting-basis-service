// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

// Because this entity is nullable (depending on "ResponsibleForVotingCards"), newly added properties might need to be migrated to be non-null
// (otherwise EF Core might have issues to include the owned entity: https://github.com/dotnet/efcore/issues/25359)
public class DomainOfInfluenceVotingCardReturnAddress
{
    public string AddressLine1 { get; set; } = string.Empty;

    public string AddressLine2 { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string AddressAddition { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}
