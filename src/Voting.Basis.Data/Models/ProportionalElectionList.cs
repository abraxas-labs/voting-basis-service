// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class ProportionalElectionList : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();

    public int BlankRowCount { get; set; }

    public int Position { get; set; }

    public int CountOfCandidates { get; set; }

    public bool CandidateCountOk { get; set; }

    public Dictionary<string, string> ListUnionDescription { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> SubListUnionDescription { get; set; } = new Dictionary<string, string>();

    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    public ICollection<ProportionalElectionCandidate> ProportionalElectionCandidates { get; set; }
        = new HashSet<ProportionalElectionCandidate>();

    public ICollection<ProportionalElectionListUnionEntry> ProportionalElectionListUnionEntries { get; set; }
        = new HashSet<ProportionalElectionListUnionEntry>();

    public ICollection<ProportionalElectionListUnion> ProportionalElectionMainListUnions { get; set; }
        = new HashSet<ProportionalElectionListUnion>();

    public ICollection<ProportionalElectionUnionListEntry> ProportionalElectionUnionListEntries { get; set; }
        = new HashSet<ProportionalElectionUnionListEntry>();

    public Guid? PartyId { get; set; }

    public DomainOfInfluenceParty? Party { get; set; }

    /// <summary>
    /// Checks whether the number of mandates equals the sum of <see cref="BlankRowCount"/> and <see cref="CountOfCandidates"/>.
    /// </summary>
    /// <param name="numberOfMandates">The number of mandates in the election. If not provided, the value is resolved from the <see cref="ProportionalElection"/>.</param>
    public void UpdateCandidateCountOk(int? numberOfMandates = null)
    {
        numberOfMandates ??= ProportionalElection.NumberOfMandates;
        CandidateCountOk = numberOfMandates == BlankRowCount + CountOfCandidates;
    }
}
