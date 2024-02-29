﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Domain;

public class DomainOfInfluenceVotingCardSwissPostData
{
    public string InvoiceReferenceNumber { get; private set; } = string.Empty;

    public string FrankingLicenceReturnNumber { get; private set; } = string.Empty;
}
