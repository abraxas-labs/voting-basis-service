// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

// Because this entity is nullable (depending on "ResponsibleForVotingCards"), newly added properties might need to be migrated to be non-null
// (otherwise EF Core might have issues to include the owned entity: https://github.com/dotnet/efcore/issues/25359)
public class DomainOfInfluenceVotingCardSwissPostData
{
    public string InvoiceReferenceNumber { get; set; } = string.Empty;

    public string FrankingLicenceReturnNumber { get; set; } = string.Empty;
}
