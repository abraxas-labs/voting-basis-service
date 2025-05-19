// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Data.Models;

public class DomainOfInfluence : BaseDomainOfInfluence, IHasSnapshotEntity<DomainOfInfluenceSnapshot>
{
    public Guid? ParentId { get; set; }

    public DomainOfInfluence? Parent { get; set; }

    public ICollection<DomainOfInfluence> Children { get; set; } = new HashSet<DomainOfInfluence>();

    public ICollection<DomainOfInfluenceCountingCircle> CountingCircles { get; set; } = new HashSet<DomainOfInfluenceCountingCircle>();

    public ICollection<Contest> Contests { get; set; } = new HashSet<Contest>();

    public ICollection<PoliticalAssembly> PoliticalAssemblies { get; set; } = new HashSet<PoliticalAssembly>();

    public ICollection<Vote> Votes { get; set; } = new HashSet<Vote>();

    public ICollection<ProportionalElection> ProportionalElections { get; set; } = new HashSet<ProportionalElection>();

    public ICollection<MajorityElection> MajorityElections { get; set; } = new HashSet<MajorityElection>();

    public ICollection<SimplePoliticalBusiness> SimplePoliticalBusinesses { get; set; } = new HashSet<SimplePoliticalBusiness>();

    public ICollection<ExportConfiguration> ExportConfigurations { get; set; } = new HashSet<ExportConfiguration>();

    public DateTime ModifiedOn { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    public bool Deleted { get; set; }

    public DomainOfInfluenceCantonDefaults CantonDefaults { get; set; }
        = new DomainOfInfluenceCantonDefaults();

    public string? LogoRef { get; set; }

    public bool HasLogo => LogoRef != null;

    public bool HasForeignerVoters { get; set; }

    public bool HasMinorVoters { get; set; }

    public PlausibilisationConfiguration? PlausibilisationConfiguration { get; set; }

    public ICollection<DomainOfInfluenceParty> Parties { get; set; } = new HashSet<DomainOfInfluenceParty>();

    public Guid? SuperiorAuthorityDomainOfInfluenceId { get; set; }

    public DomainOfInfluence? SuperiorAuthorityDomainOfInfluence { get; set; }

    public ICollection<DomainOfInfluence> SubAuthorityDomainOfInfluences { get; set; } = new HashSet<DomainOfInfluence>();

    public void SortExportConfigurations()
    {
        ExportConfigurations = ExportConfigurations.OrderBy(x => x.Description).ToList();
    }
}
