// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Models;

public class ProportionalElectionImportCandidate : ProportionalElectionCandidate
{
    /// <summary>
    /// Gets or sets the source party short by each language.
    /// This is equivalent by PartyAffiliationShort of eCH.
    /// It is needed to allow the user to map each party of the source system to a party of this system.
    /// </summary>
    public Dictionary<string, string>? SourcePartyShort { get; set; }

    /// <summary>
    /// Gets or sets the source party by each language.
    /// This is equivalent by PartyAffiliation of eCH.
    /// It is needed to allow the user to map each party of the source system to a party of this system.
    /// </summary>
    public Dictionary<string, string>? SourceParty { get; set; }
}
