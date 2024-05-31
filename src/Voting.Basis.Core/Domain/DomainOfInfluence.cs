// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

/// <summary>
/// The domain of influence aggregate (in german: Wahlkreis).
/// </summary>
public class DomainOfInfluence
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID responsible for this domain of influence.
    /// </summary>
    public string SecureConnectId { get; set; } = string.Empty;

    public string AuthorityName { get; set; } = string.Empty;

    public DomainOfInfluenceType Type { get; set; }

    public DomainOfInfluenceCanton Canton { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this domain of influence is responsible for its voting cards.
    /// This is mainly important for VOTING Stimmunterlagen.
    /// </summary>
    public bool ResponsibleForVotingCards { get; set; }

    /// <summary>
    /// Gets or sets the BFS (number from the "Bundesamt für Statistik) for this domain of influence.
    /// </summary>
    public string Bfs { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain of influence code, which is an arbitrary entered value by the user.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort number, which is an arbitrary entered value by the user.
    /// </summary>
    public int SortNumber { get; set; }

    public string NameForProtocol { get; set; } = string.Empty;

    public ContactPerson ContactPerson { get; set; } = new();

    public DomainOfInfluenceVotingCardReturnAddress? ReturnAddress { get; set; }

    public DomainOfInfluenceVotingCardPrintData? PrintData { get; set; }

    public DomainOfInfluenceVotingCardSwissPostData? SwissPostData { get; set; }

    public PlausibilisationConfiguration? PlausibilisationConfiguration { get; set; }

    public IReadOnlyCollection<DomainOfInfluenceParty> Parties { get; set; } = Array.Empty<DomainOfInfluenceParty>();

    public Guid? ParentId { get; set; }

    public IReadOnlyCollection<ExportConfiguration> ExportConfigurations { get; set; } = Array.Empty<ExportConfiguration>();

    public bool ExternalPrintingCenter { get; set; }

    public string ExternalPrintingCenterEaiMessageType { get; set; } = string.Empty;

    public string SapCustomerOrderNumber { get; set; } = string.Empty;

    public bool VirtualTopLevel { get; set; }

    public bool ViewCountingCirclePartialResults { get; set; }

    public VotingCardColor VotingCardColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether VOTING Stimmregister is enabled.
    /// </summary>
    public bool ElectoralRegistrationEnabled { get; set; }
}
