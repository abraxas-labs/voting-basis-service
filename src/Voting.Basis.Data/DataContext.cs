// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<EventProcessingState> EventProcessingStates { get; set; } = null!;

    // nullables see https://docs.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types
    public DbSet<CountingCircle> CountingCircles { get; set; } = null!;

    public DbSet<Authority> Authorities { get; set; } = null!;

    public DbSet<CountingCircleContactPerson> CountingCircleContactPersons { get; set; } = null!;

    public DbSet<CountingCircleElectorate> CountingCircleElectorates { get; set; } = null!;

    public DbSet<DomainOfInfluence> DomainOfInfluences { get; set; } = null!;

    public DbSet<DomainOfInfluenceCountingCircle> DomainOfInfluenceCountingCircles { get; set; } = null!;

    public DbSet<Contest> Contests { get; set; } = null!;

    public DbSet<ContestCountingCircleOption> ContestCountingCircleOptions { get; set; } = null!;

    public DbSet<PreconfiguredContestDate> PreconfiguredContestDates { get; set; } = null!;

    public DbSet<Vote> Votes { get; set; } = null!;

    public DbSet<Ballot> Ballots { get; set; } = null!;

    public DbSet<BallotQuestion> BallotQuestions { get; set; } = null!;

    public DbSet<TieBreakQuestion> TieBreakQuestions { get; set; } = null!;

    public DbSet<ProportionalElection> ProportionalElections { get; set; } = null!;

    public DbSet<ProportionalElectionUnion> ProportionalElectionUnions { get; set; } = null!;

    public DbSet<ProportionalElectionUnionEntry> ProportionalElectionUnionEntries { get; set; } = null!;

    public DbSet<ProportionalElectionList> ProportionalElectionLists { get; set; } = null!;

    public DbSet<ProportionalElectionListUnion> ProportionalElectionListUnions { get; set; } = null!;

    public DbSet<ProportionalElectionListUnionEntry> ProportionalElectionListUnionEntries { get; set; } = null!;

    public DbSet<ProportionalElectionCandidate> ProportionalElectionCandidates { get; set; } = null!;

    public DbSet<ProportionalElectionUnionList> ProportionalElectionUnionLists { get; set; } = null!;

    public DbSet<ProportionalElectionUnionListEntry> ProportionalElectionUnionListEntries { get; set; } = null!;

    public DbSet<MajorityElection> MajorityElections { get; set; } = null!;

    public DbSet<MajorityElectionUnion> MajorityElectionUnions { get; set; } = null!;

    public DbSet<MajorityElectionUnionEntry> MajorityElectionUnionEntries { get; set; } = null!;

    public DbSet<MajorityElectionCandidate> MajorityElectionCandidates { get; set; } = null!;

    public DbSet<SecondaryMajorityElection> SecondaryMajorityElections { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionCandidate> SecondaryMajorityElectionCandidates { get; set; } = null!;

    public DbSet<MajorityElectionBallotGroup> MajorityElectionBallotGroups { get; set; } = null!;

    public DbSet<MajorityElectionBallotGroupEntry> MajorityElectionBallotGroupEntries { get; set; } = null!;

    public DbSet<MajorityElectionBallotGroupEntryCandidate> MajorityElectionBallotGroupEntryCandidates { get; set; } = null!;

    public DbSet<ElectionGroup> ElectionGroups { get; set; } = null!;

    public DbSet<DomainOfInfluencePermissionEntry> DomainOfInfluencePermissions { get; set; } = null!;

    public DbSet<DomainOfInfluenceHierarchy> DomainOfInfluenceHierarchies { get; set; } = null!;

    public DbSet<CountingCircleSnapshot> CountingCircleSnapshots { get; set; } = null!;

    public DbSet<DomainOfInfluenceSnapshot> DomainOfInfluenceSnapshots { get; set; } = null!;

    public DbSet<DomainOfInfluenceCountingCircleSnapshot> DomainOfInfluenceCountingCircleSnapshots { get; set; } = null!;

    public DbSet<AuthoritySnapshot> AuthoritySnapshots { get; set; } = null!;

    public DbSet<CountingCircleContactPersonSnapshot> CountingCircleContactPersonSnapshots { get; set; } = null!;

    public DbSet<CantonSettings> CantonSettings { get; set; } = null!;

    public DbSet<CantonSettingsVotingCardChannel> CantonSettingsVotingCardChannels { get; set; } = null!;

    public DbSet<CountingCirclesMerger> CountingCirclesMergers { get; set; } = null!;

    public DbSet<ExportConfiguration> ExportConfigurations { get; set; } = null!;

    public DbSet<PlausibilisationConfiguration> PlausibilisationConfigurations { get; set; } = null!;

    public DbSet<ComparisonVoterParticipationConfiguration> ComparisonVoterParticipationConfigurations { get; set; } = null!;

    public DbSet<ComparisonCountOfVotersConfiguration> ComparisonCountOfVotersConfigurations { get; set; } = null!;

    public DbSet<ComparisonVotingChannelConfiguration> ComparisonVotingChannelConfigurations { get; set; } = null!;

    public DbSet<DomainOfInfluenceParty> DomainOfInfluenceParties { get; set; } = null!;

    public DbSet<SimplePoliticalBusiness> SimplePoliticalBusiness { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
