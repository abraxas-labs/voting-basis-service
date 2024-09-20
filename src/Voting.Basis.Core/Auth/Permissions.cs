// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Auth;

public static class Permissions
{
    private const string CreateSuffix = ":create";
    private const string UpdateSuffix = ":update";
    private const string ReadSuffix = ":read";
    private const string DeleteSuffix = ":delete";

    // This suffix allows to access the resource when the tenant of the users matches the tenant of the resource
    private const string ReadSameTenantSuffix = ReadSuffix + "-same-tenant";
    private const string UpdateSameTenantSuffix = UpdateSuffix + "-same-tenant";

    // This suffix allows to access the resource when the "canton matches"
    private const string CreateSameCantonSuffix = CreateSuffix + "-same-canton";
    private const string ReadSameCantonSuffix = ReadSuffix + "-same-canton";
    private const string UpdateSameCantonSuffix = UpdateSuffix + "-same-canton";
    private const string DeleteSameCantonSuffix = DeleteSuffix + "-same-canton";

    // This suffix allows to access admin management resources
    private const string ReadAdminManagementSuffix = ReadSuffix + "-admin-management";

    // Used when the "normal" permission (ex. 'read') allows access only to specific resources, while the  '-all' allows access to all resources
    private const string CreateAllSuffix = CreateSuffix + "-all";
    private const string ReadAllSuffix = ReadSuffix + "-all";
    private const string UpdateAllSuffix = UpdateSuffix + "-all";
    private const string DeleteAllSuffix = DeleteSuffix + "-all";

    public static class DomainOfInfluence
    {
        public const string CreateAll = Prefix + CreateAllSuffix;
        public const string CreateSameCanton = Prefix + CreateSameCantonSuffix;
        public const string UpdateAll = Prefix + UpdateAllSuffix;
        public const string UpdateSameCanton = Prefix + UpdateSameCantonSuffix;
        public const string UpdateSameTenant = Prefix + UpdateSameTenantSuffix;
        public const string ReadSameTenant = Prefix + ReadSameTenantSuffix;
        public const string ReadSameCanton = Prefix + ReadSameCantonSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;
        public const string DeleteSameCanton = Prefix + DeleteSameCantonSuffix;
        public const string DeleteAll = Prefix + DeleteAllSuffix;

        private const string Prefix = "DomainOfInfluence";
    }

    public static class DomainOfInfluenceHierarchy
    {
        public const string ReadSameTenant = Prefix + ReadSameTenantSuffix;
        public const string ReadSameCanton = Prefix + ReadSameCantonSuffix;
        public const string ReadAdminManagement = Prefix + ReadAdminManagementSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;
        public const string UpdateSameCanton = Prefix + UpdateSameCantonSuffix;
        public const string UpdateAll = Prefix + UpdateAllSuffix;

        private const string Prefix = "DomainOfInfluence.Hierarchy";
    }

    public static class DomainOfInfluenceLogo
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "DomainOfInfluence.Logo";
    }

    public static class CantonSettings
    {
        public const string Create = Prefix + CreateSuffix;
        public const string UpdateAll = Prefix + UpdateAllSuffix;
        public const string ReadSameTenant = Prefix + ReadSameTenantSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;

        private const string Prefix = "CantonSettings";
    }

    public static class CountingCircle
    {
        public const string CreateAll = Prefix + CreateAllSuffix;
        public const string CreateSameCanton = Prefix + CreateSameCantonSuffix;
        public const string UpdateSameTenant = Prefix + UpdateSameTenantSuffix;
        public const string UpdateSameCanton = Prefix + UpdateSameCantonSuffix;
        public const string UpdateAll = Prefix + UpdateAllSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string ReadSameCanton = Prefix + ReadSameCantonSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;
        public const string DeleteSameCanton = Prefix + DeleteSameCantonSuffix;
        public const string DeleteAll = Prefix + DeleteSuffix;
        public const string MergeSameCanton = Prefix + ":merge-same-canton";
        public const string MergeAll = Prefix + ":merge-all";

        private const string Prefix = "CountingCircle";
    }

    public static class Contest
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string ReadTenantHierarchy = Prefix + ReadSuffix + "-tenant-hierarchy";
        public const string ReadSameCanton = Prefix + ReadSameCantonSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "Contest";
    }

    public static class PoliticalAssembly
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string ReadTenantHierarchy = Prefix + ReadSuffix + "-tenant-hierarchy";
        public const string ReadSameCanton = Prefix + ReadSameCantonSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "PoliticalAssembly";
    }

    public static class Vote
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "Vote";
    }

    public static class VoteBallot
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "VoteBallot";
    }

    public static class ProportionalElection
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "ProportionalElection";
    }

    public static class ProportionalElectionList
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "ProportionalElection.List";
    }

    public static class ProportionalElectionListUnion
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "ProportionalElection.ListUnion";
    }

    public static class ProportionalElectionCandidate
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "ProportionalElection.Candidate";
    }

    public static class ProportionalElectionUnion
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "ProportionalElection.Union";
    }

    public static class MajorityElection
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "MajorityElection";
    }

    public static class MajorityElectionCandidate
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "MajorityElection.Candidate";
    }

    public static class MajorityElectionBallotGroup
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "MajorityElection.BallotGroup";
    }

    public static class MajorityElectionUnion
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "MajorityElection.Union";
    }

    public static class SecondaryMajorityElection
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "SecondaryMajorityElection";
    }

    public static class SecondaryMajorityElectionCandidate
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "SecondaryMajorityElection.Candidate";
    }

    public static class ElectionGroup
    {
        public const string Update = Prefix + UpdateSuffix;
        public const string Read = Prefix + ReadSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;

        private const string Prefix = "ElectionGroup";
    }

    public static class EventLog
    {
        public const string ReadSameTenant = Prefix + ReadSameTenantSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;

        private const string Prefix = "EventLog";
    }

    public static class Import
    {
        public const string ImportData = Prefix + ":import";

        private const string Prefix = "Import";
    }

    public static class Export
    {
        public const string ExportData = Prefix + ":export";
        public const string ExportAllPoliticalBusinesses = Prefix + ":export-all-political-businesses";

        private const string Prefix = "Export";
    }
}
