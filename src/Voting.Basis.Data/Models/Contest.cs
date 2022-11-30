// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class Contest : BaseEntity
{
    public DateTime Date { get; set; }

    public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();

    public DateTime EndOfTestingPhase { get; set; }

    public DateTime? ArchivePer { get; set; }

    public DateTime PastLockPer { get; set; }

    public ContestState State { get; set; } = ContestState.TestingPhase;

    public Guid DomainOfInfluenceId { get; set; }

    public DomainOfInfluence DomainOfInfluence { get; set; } = null!; // set by ef

    public bool EVoting { get; set; }

    public DateTime? EVotingFrom { get; set; }

    public DateTime? EVotingTo { get; set; }

    public ICollection<Vote> Votes { get; set; } = new HashSet<Vote>();

    public ICollection<ProportionalElection> ProportionalElections { get; set; } = new HashSet<ProportionalElection>();

    public ICollection<MajorityElection> MajorityElections { get; set; } = new HashSet<MajorityElection>();

    public ICollection<ProportionalElectionUnion> ProportionalElectionUnions { get; set; } = new HashSet<ProportionalElectionUnion>();

    public ICollection<MajorityElectionUnion> MajorityElectionUnions { get; set; } = new HashSet<MajorityElectionUnion>();

    public ICollection<ContestCountingCircleOption> CountingCircleOptions { get; set; } = new HashSet<ContestCountingCircleOption>();

    public ICollection<SimplePoliticalBusiness> SimplePoliticalBusinesses { get; set; } = new HashSet<SimplePoliticalBusiness>();

    public Guid? PreviousContestId { get; set; }

    public Contest? PreviousContest { get; set; }

    public ICollection<Contest> PreviousContestOwners { get; set; } = new HashSet<Contest>();

    [NotMapped]
    public List<PoliticalBusiness> PoliticalBusinesses
    {
        get
        {
            return Votes.Cast<PoliticalBusiness>()
                .Concat(MajorityElections)
                .Concat(ProportionalElections)
                .Concat(MajorityElections.SelectMany(me => me.SecondaryMajorityElections))
                .OrderBy(pb => pb.DomainOfInfluence?.Type)
                .ThenBy(pb => pb.PoliticalBusinessNumber)
                .ToList();
        }

        set
        {
            Votes.Clear();
            ProportionalElections.Clear();
            MajorityElections.Clear();

            foreach (var item in value)
            {
                switch (item)
                {
                    case Vote v:
                        Votes.Add(v);
                        break;
                    case ProportionalElection p:
                        ProportionalElections.Add(p);
                        break;
                    case MajorityElection m:
                        MajorityElections.Add(m);
                        break;
                }
            }
        }
    }

    [NotMapped]
    public List<PoliticalBusinessUnion> PoliticalBusinessUnions
    {
        get
        {
            return ProportionalElectionUnions.Cast<PoliticalBusinessUnion>()
                .Concat(MajorityElectionUnions)
                .OrderBy(u => u.Description)
                .ToList();
        }

        set
        {
            ProportionalElectionUnions.Clear();
            MajorityElectionUnions.Clear();

            foreach (var item in value)
            {
                switch (item)
                {
                    case ProportionalElectionUnion p:
                        ProportionalElectionUnions.Add(p);
                        break;
                    case MajorityElectionUnion m:
                        MajorityElectionUnions.Add(m);
                        break;
                }
            }
        }
    }

    public bool TestingPhaseEnded => State.TestingPhaseEnded();
}
