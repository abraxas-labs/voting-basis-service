// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

// Because this entity is nullable (depending on "ResponsibleForVotingCards"), newly added properties might need to be migrated to be non-null
// (otherwise EF Core might have issues to include the owned entity: https://github.com/dotnet/efcore/issues/25359)
public class DomainOfInfluenceVotingCardPrintData
{
    public VotingCardShippingFranking ShippingAway { get; set; }

    public VotingCardShippingFranking ShippingReturn { get; set; }

    public VotingCardShippingMethod ShippingMethod { get; set; }

    public bool ShippingVotingCardsToDeliveryAddress { get; set; }
}
