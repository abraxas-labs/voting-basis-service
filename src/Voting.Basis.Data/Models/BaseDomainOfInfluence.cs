// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public abstract class BaseDomainOfInfluence : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public string SecureConnectId { get; set; } = string.Empty;

    public string AuthorityName { get; set; } = string.Empty;

    public DomainOfInfluenceType Type { get; set; }

    public DomainOfInfluenceCanton Canton { get; set; }

    public bool ResponsibleForVotingCards { get; set; }

    public bool ExternalPrintingCenter { get; set; }

    public string ExternalPrintingCenterEaiMessageType { get; set; } = string.Empty;

    public string SapCustomerOrderNumber { get; set; } = string.Empty;

    public string Bfs { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int SortNumber { get; set; }

    public string NameForProtocol { get; set; } = string.Empty;

    public ContactPerson ContactPerson { get; set; } = new ContactPerson();

    public DomainOfInfluenceVotingCardReturnAddress? ReturnAddress { get; set; }

    public DomainOfInfluenceVotingCardPrintData? PrintData { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
}
