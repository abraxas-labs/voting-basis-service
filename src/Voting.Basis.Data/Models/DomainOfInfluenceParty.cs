// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class DomainOfInfluenceParty : BaseEntity
{
    public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();

    public Guid DomainOfInfluenceId { get; set; }

    public DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public ICollection<ProportionalElectionCandidate> ProportionalElectionCandidates { get; set; } = new HashSet<ProportionalElectionCandidate>();

    public bool Deleted { get; set; }

    public ICollection<ProportionalElectionList> ProportionalElectionLists { get; set; } = new HashSet<ProportionalElectionList>();
}
