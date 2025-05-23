// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Lib.Iam.Authorization;

namespace Voting.Basis.Core.Auth;

public class PermissionProvider : IPermissionProvider
{
    private readonly Dictionary<string, HashSet<string>> _permissionsPerRole = new();

    public PermissionProvider()
    {
        _permissionsPerRole[Roles.Admin] = new HashSet<string>
        {
            Permissions.CantonSettings.Create,
            Permissions.CantonSettings.UpdateAll,
            Permissions.CantonSettings.ReadAll,

            Permissions.DomainOfInfluenceLogo.Update,
            Permissions.DomainOfInfluenceLogo.Read,
            Permissions.DomainOfInfluenceLogo.Delete,

            Permissions.DomainOfInfluenceHierarchy.ReadAll,

            Permissions.DomainOfInfluence.ReadAll,

            Permissions.CountingCircle.ReadAll,

            Permissions.Contest.ReadAll,

            Permissions.PoliticalAssembly.ReadAll,
            Permissions.EventLog.ReadAll,
            Permissions.EventLog.Watch,

            Permissions.Export.ExportData,
            Permissions.Export.ExportAllPoliticalBusinesses,
        };
        _permissionsPerRole[Roles.CantonAdmin] = new HashSet<string>
        {
            Permissions.CantonSettings.ReadSameTenant,

            Permissions.DomainOfInfluenceLogo.Update,
            Permissions.DomainOfInfluenceLogo.Read,
            Permissions.DomainOfInfluenceLogo.Delete,

            Permissions.DomainOfInfluenceHierarchy.ReadSameCanton,
            Permissions.DomainOfInfluenceHierarchy.UpdateSameCanton,

            Permissions.DomainOfInfluence.CreateSameCanton,
            Permissions.DomainOfInfluence.UpdateSameCanton,
            Permissions.DomainOfInfluence.DeleteSameCanton,
            Permissions.DomainOfInfluence.ReadSameCanton,

            Permissions.CountingCircle.ReadSameCanton,
            Permissions.CountingCircle.CreateSameCanton,
            Permissions.CountingCircle.UpdateSameCanton,
            Permissions.CountingCircle.DeleteSameCanton,
            Permissions.CountingCircle.MergeSameCanton,

            Permissions.Contest.Create,
            Permissions.Contest.Update,
            Permissions.Contest.Delete,
            Permissions.Contest.ReadSameCanton,

            Permissions.PoliticalAssembly.Create,
            Permissions.PoliticalAssembly.Update,
            Permissions.PoliticalAssembly.Delete,
            Permissions.PoliticalAssembly.ReadSameCanton,

            Permissions.PoliticalBusiness.ReadActionsTenantSameCanton,
            Permissions.PoliticalBusiness.WriteActionsTenantSameCanton,
            Permissions.PoliticalBusiness.ReadActionsSameTenant,
            Permissions.PoliticalBusiness.WriteActionsSameTenant,

            Permissions.PoliticalBusinessUnion.ActionsTenantSameCanton,

            Permissions.Vote.Create,
            Permissions.Vote.Update,
            Permissions.Vote.Read,
            Permissions.Vote.Delete,

            Permissions.VoteBallot.Create,
            Permissions.VoteBallot.Update,
            Permissions.VoteBallot.Read,
            Permissions.VoteBallot.Delete,

            Permissions.ProportionalElection.Create,
            Permissions.ProportionalElection.Update,
            Permissions.ProportionalElection.Read,
            Permissions.ProportionalElection.Delete,

            Permissions.ProportionalElectionList.Create,
            Permissions.ProportionalElectionList.Update,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionList.Delete,

            Permissions.ProportionalElectionListUnion.Create,
            Permissions.ProportionalElectionListUnion.Update,
            Permissions.ProportionalElectionListUnion.Read,
            Permissions.ProportionalElectionListUnion.Delete,

            Permissions.ProportionalElectionCandidate.Create,
            Permissions.ProportionalElectionCandidate.Update,
            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionCandidate.Delete,

            Permissions.ProportionalElectionUnion.Create,
            Permissions.ProportionalElectionUnion.Update,
            Permissions.ProportionalElectionUnion.Read,
            Permissions.ProportionalElectionUnion.Delete,

            Permissions.MajorityElection.Create,
            Permissions.MajorityElection.Update,
            Permissions.MajorityElection.Read,
            Permissions.MajorityElection.Delete,

            Permissions.MajorityElectionCandidate.Create,
            Permissions.MajorityElectionCandidate.Update,
            Permissions.MajorityElectionCandidate.Read,
            Permissions.MajorityElectionCandidate.Delete,

            Permissions.MajorityElectionBallotGroup.Create,
            Permissions.MajorityElectionBallotGroup.Update,
            Permissions.MajorityElectionBallotGroup.Read,
            Permissions.MajorityElectionBallotGroup.Delete,

            Permissions.MajorityElectionUnion.Create,
            Permissions.MajorityElectionUnion.Update,
            Permissions.MajorityElectionUnion.Read,
            Permissions.MajorityElectionUnion.Delete,

            Permissions.SecondaryMajorityElection.Create,
            Permissions.SecondaryMajorityElection.Update,
            Permissions.SecondaryMajorityElection.Read,
            Permissions.SecondaryMajorityElection.Delete,

            Permissions.SecondaryMajorityElectionCandidate.Create,
            Permissions.SecondaryMajorityElectionCandidate.Update,
            Permissions.SecondaryMajorityElectionCandidate.Read,
            Permissions.SecondaryMajorityElectionCandidate.Delete,

            Permissions.EventLog.ReadSameTenant,
            Permissions.EventLog.Watch,

            Permissions.Import.ImportData,

            Permissions.Export.ExportData,
            Permissions.Export.ExportAllPoliticalBusinesses,

            Permissions.Country.Read,
        };
        _permissionsPerRole[Roles.CantonAdminReadOnly] = new HashSet<string>
        {
            Permissions.CantonSettings.ReadSameTenant,

            Permissions.DomainOfInfluenceLogo.Read,

            Permissions.DomainOfInfluenceHierarchy.ReadSameCanton,

            Permissions.DomainOfInfluence.ReadSameCanton,

            Permissions.CountingCircle.ReadSameCanton,

            Permissions.Contest.ReadSameCanton,

            Permissions.PoliticalAssembly.ReadSameCanton,

            Permissions.PoliticalBusiness.ReadActionsTenantSameCanton,
            Permissions.PoliticalBusiness.ReadActionsSameTenant,

            Permissions.PoliticalBusinessUnion.ActionsTenantSameCanton,

            Permissions.Vote.Read,

            Permissions.VoteBallot.Read,

            Permissions.ProportionalElection.Read,

            Permissions.ProportionalElectionList.Read,

            Permissions.ProportionalElectionListUnion.Read,

            Permissions.ProportionalElectionCandidate.Read,

            Permissions.ProportionalElectionUnion.Read,

            Permissions.MajorityElection.Read,

            Permissions.MajorityElectionCandidate.Read,

            Permissions.MajorityElectionBallotGroup.Read,

            Permissions.MajorityElectionUnion.Read,

            Permissions.SecondaryMajorityElection.Read,

            Permissions.SecondaryMajorityElectionCandidate.Read,

            Permissions.EventLog.ReadSameTenant,
            Permissions.EventLog.Watch,

            Permissions.Export.ExportData,
            Permissions.Export.ExportAllPoliticalBusinesses,

            Permissions.Country.Read,
        };
        _permissionsPerRole[Roles.ElectionAdmin] = new HashSet<string>
        {
            Permissions.CantonSettings.ReadSameTenant,

            Permissions.DomainOfInfluenceLogo.Update,
            Permissions.DomainOfInfluenceLogo.Read,
            Permissions.DomainOfInfluenceLogo.Delete,

            Permissions.DomainOfInfluence.UpdateSameTenant,
            Permissions.DomainOfInfluence.ReadSameTenant,

            Permissions.DomainOfInfluenceHierarchy.ReadSameTenant,

            Permissions.CountingCircle.Read,
            Permissions.CountingCircle.UpdateSameTenant,

            Permissions.Contest.Create,
            Permissions.Contest.Update,
            Permissions.Contest.Delete,
            Permissions.Contest.ReadTenantHierarchy,

            Permissions.PoliticalAssembly.Create,
            Permissions.PoliticalAssembly.Update,
            Permissions.PoliticalAssembly.Delete,
            Permissions.PoliticalAssembly.ReadTenantHierarchy,

            Permissions.PoliticalBusiness.ReadActionsSameTenant,
            Permissions.PoliticalBusiness.WriteActionsSameTenant,

            Permissions.Vote.Create,
            Permissions.Vote.Update,
            Permissions.Vote.Read,
            Permissions.Vote.Delete,

            Permissions.VoteBallot.Create,
            Permissions.VoteBallot.Update,
            Permissions.VoteBallot.Read,
            Permissions.VoteBallot.Delete,

            Permissions.ProportionalElection.Create,
            Permissions.ProportionalElection.Update,
            Permissions.ProportionalElection.Read,
            Permissions.ProportionalElection.Delete,

            Permissions.ProportionalElectionList.Create,
            Permissions.ProportionalElectionList.Update,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionList.Delete,

            Permissions.ProportionalElectionListUnion.Create,
            Permissions.ProportionalElectionListUnion.Update,
            Permissions.ProportionalElectionListUnion.Read,
            Permissions.ProportionalElectionListUnion.Delete,

            Permissions.ProportionalElectionCandidate.Create,
            Permissions.ProportionalElectionCandidate.Update,
            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionCandidate.Delete,

            Permissions.ProportionalElectionUnion.Create,
            Permissions.ProportionalElectionUnion.Update,
            Permissions.ProportionalElectionUnion.Read,
            Permissions.ProportionalElectionUnion.Delete,

            Permissions.MajorityElection.Create,
            Permissions.MajorityElection.Update,
            Permissions.MajorityElection.Read,
            Permissions.MajorityElection.Delete,

            Permissions.MajorityElectionCandidate.Create,
            Permissions.MajorityElectionCandidate.Update,
            Permissions.MajorityElectionCandidate.Read,
            Permissions.MajorityElectionCandidate.Delete,

            Permissions.MajorityElectionBallotGroup.Create,
            Permissions.MajorityElectionBallotGroup.Update,
            Permissions.MajorityElectionBallotGroup.Read,
            Permissions.MajorityElectionBallotGroup.Delete,

            Permissions.MajorityElectionUnion.Create,
            Permissions.MajorityElectionUnion.Update,
            Permissions.MajorityElectionUnion.Read,
            Permissions.MajorityElectionUnion.Delete,

            Permissions.SecondaryMajorityElection.Create,
            Permissions.SecondaryMajorityElection.Update,
            Permissions.SecondaryMajorityElection.Read,
            Permissions.SecondaryMajorityElection.Delete,

            Permissions.SecondaryMajorityElectionCandidate.Create,
            Permissions.SecondaryMajorityElectionCandidate.Update,
            Permissions.SecondaryMajorityElectionCandidate.Read,
            Permissions.SecondaryMajorityElectionCandidate.Delete,

            Permissions.EventLog.ReadSameTenant,
            Permissions.EventLog.Watch,

            Permissions.Import.ImportData,

            Permissions.Export.ExportData,

            Permissions.Country.Read,
        };
        _permissionsPerRole[Roles.ElectionAdminReadOnly] = new HashSet<string>
        {
            Permissions.CantonSettings.ReadSameTenant,

            Permissions.DomainOfInfluenceLogo.Read,

            Permissions.DomainOfInfluence.ReadSameTenant,

            Permissions.DomainOfInfluenceHierarchy.ReadSameTenant,

            Permissions.CountingCircle.Read,

            Permissions.Contest.ReadTenantHierarchy,

            Permissions.PoliticalAssembly.ReadTenantHierarchy,

            Permissions.PoliticalBusiness.ReadActionsSameTenant,

            Permissions.Vote.Read,

            Permissions.VoteBallot.Read,

            Permissions.ProportionalElection.Read,

            Permissions.ProportionalElectionList.Read,

            Permissions.ProportionalElectionListUnion.Read,

            Permissions.ProportionalElectionCandidate.Read,

            Permissions.ProportionalElectionUnion.Read,

            Permissions.MajorityElection.Read,

            Permissions.MajorityElectionCandidate.Read,

            Permissions.MajorityElectionBallotGroup.Read,

            Permissions.MajorityElectionUnion.Read,

            Permissions.SecondaryMajorityElection.Read,

            Permissions.SecondaryMajorityElectionCandidate.Read,

            Permissions.EventLog.ReadSameTenant,
            Permissions.EventLog.Watch,

            Permissions.Export.ExportData,

            Permissions.Country.Read,
        };
        _permissionsPerRole[Roles.ElectionSupporter] = new HashSet<string>
        {
            Permissions.CantonSettings.ReadSameTenant,

            Permissions.DomainOfInfluenceLogo.Update,
            Permissions.DomainOfInfluenceLogo.Read,
            Permissions.DomainOfInfluenceLogo.Delete,

            Permissions.DomainOfInfluence.UpdateSameTenant,
            Permissions.DomainOfInfluence.ReadSameTenant,

            Permissions.DomainOfInfluenceHierarchy.ReadSameTenant,

            Permissions.CountingCircle.Read,
            Permissions.CountingCircle.UpdateSameTenant,

            Permissions.Contest.ReadTenantHierarchy,

            Permissions.PoliticalAssembly.ReadTenantHierarchy,

            Permissions.PoliticalBusiness.ReadActionsSameTenant,
            Permissions.PoliticalBusiness.WriteActionsSameTenant,

            Permissions.Vote.Create,
            Permissions.Vote.Update,
            Permissions.Vote.Read,
            Permissions.Vote.Delete,

            Permissions.VoteBallot.Create,
            Permissions.VoteBallot.Update,
            Permissions.VoteBallot.Read,
            Permissions.VoteBallot.Delete,

            Permissions.ProportionalElection.Create,
            Permissions.ProportionalElection.Update,
            Permissions.ProportionalElection.Read,
            Permissions.ProportionalElection.Delete,

            Permissions.ProportionalElectionList.Create,
            Permissions.ProportionalElectionList.Update,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionList.Delete,

            Permissions.ProportionalElectionListUnion.Create,
            Permissions.ProportionalElectionListUnion.Update,
            Permissions.ProportionalElectionListUnion.Read,
            Permissions.ProportionalElectionListUnion.Delete,

            Permissions.ProportionalElectionCandidate.Create,
            Permissions.ProportionalElectionCandidate.Update,
            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionCandidate.Delete,

            Permissions.ProportionalElectionUnion.Create,
            Permissions.ProportionalElectionUnion.Update,
            Permissions.ProportionalElectionUnion.Read,
            Permissions.ProportionalElectionUnion.Delete,

            Permissions.MajorityElection.Create,
            Permissions.MajorityElection.Update,
            Permissions.MajorityElection.Read,
            Permissions.MajorityElection.Delete,

            Permissions.MajorityElectionCandidate.Create,
            Permissions.MajorityElectionCandidate.Update,
            Permissions.MajorityElectionCandidate.Read,
            Permissions.MajorityElectionCandidate.Delete,

            Permissions.MajorityElectionBallotGroup.Create,
            Permissions.MajorityElectionBallotGroup.Update,
            Permissions.MajorityElectionBallotGroup.Read,
            Permissions.MajorityElectionBallotGroup.Delete,

            Permissions.MajorityElectionUnion.Create,
            Permissions.MajorityElectionUnion.Update,
            Permissions.MajorityElectionUnion.Read,
            Permissions.MajorityElectionUnion.Delete,

            Permissions.SecondaryMajorityElection.Create,
            Permissions.SecondaryMajorityElection.Update,
            Permissions.SecondaryMajorityElection.Read,
            Permissions.SecondaryMajorityElection.Delete,

            Permissions.SecondaryMajorityElectionCandidate.Create,
            Permissions.SecondaryMajorityElectionCandidate.Update,
            Permissions.SecondaryMajorityElectionCandidate.Read,
            Permissions.SecondaryMajorityElectionCandidate.Delete,

            Permissions.EventLog.ReadSameTenant,
            Permissions.EventLog.Watch,

            Permissions.Import.ImportData,

            Permissions.Export.ExportData,

            Permissions.Country.Read,
        };
        _permissionsPerRole[Roles.ApiReader] = new HashSet<string>
        {
            Permissions.DomainOfInfluenceHierarchy.ReadAll,
        };
        _permissionsPerRole[Roles.ApiReaderDoi] = new HashSet<string>
        {
            Permissions.DomainOfInfluenceHierarchy.ReadAdminManagement,
        };
    }

    public IReadOnlyCollection<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>();
        foreach (var role in roles)
        {
            if (_permissionsPerRole.TryGetValue(role, out var rolePermissions))
            {
                permissions.UnionWith(rolePermissions);
            }
        }

        return permissions;
    }
}
