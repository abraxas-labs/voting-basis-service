// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

public class DomainOfInfluenceVotingCardPrintData
{
    public VotingCardShippingFranking ShippingAway { get; set; }

    public VotingCardShippingFranking ShippingReturn { get; set; }

    public VotingCardShippingMethod ShippingMethod { get; set; }
}
