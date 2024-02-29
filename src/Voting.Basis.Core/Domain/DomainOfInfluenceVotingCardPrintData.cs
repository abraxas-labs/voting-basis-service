// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Core.Domain;

public class DomainOfInfluenceVotingCardPrintData
{
    public VotingCardShippingFranking ShippingAway { get; private set; }

    public VotingCardShippingFranking ShippingReturn { get; private set; }

    public VotingCardShippingMethod ShippingMethod { get; private set; }

    public bool ShippingVotingCardsToDeliveryAddress { get; private set; }
}
