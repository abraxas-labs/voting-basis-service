﻿// (c) Copyright by Abraxas Informatik AG
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

    public DomainOfInfluenceVotingCardSwissPostData? SwissPostData { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    public bool VirtualTopLevel { get; set; }

    public bool ViewCountingCirclePartialResults { get; set; }

    public VotingCardColor VotingCardColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether VOTING Stimmregister is enabled.
    /// </summary>
    public bool ElectoralRegistrationEnabled { get; set; }

    public bool ElectoralRegisterMultipleEnabled { get; set; }

    public bool StistatMunicipality { get; set; }

    public bool PublishResultsDisabled { get; set; }

    public bool VotingCardFlatRateDisabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether domain of influences lower in the hierarchy than this should not be displayed in reports.
    /// </summary>
    public bool HideLowerDomainOfInfluencesInReports { get; set; }

    public bool ECollectingEnabled { get; set; }

    public int ECollectingMinSignatureCount { get; set; }

    public int ECollectingMaxElectronicSignaturePercent { get; set; }
}
