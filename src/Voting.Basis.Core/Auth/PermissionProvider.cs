// (c) Copyright 2024 by Abraxas Informatik AG
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
            Permissions.CantonSettings.Update,
            Permissions.CantonSettings.Read,
            Permissions.CantonSettings.ReadAll,

            Permissions.DomainOfInfluenceLogo.Update,
            Permissions.DomainOfInfluenceLogo.Read,
            Permissions.DomainOfInfluenceLogo.Delete,

            Permissions.DomainOfInfluenceHierarchy.Read,
            Permissions.DomainOfInfluenceHierarchy.ReadAll,
            Permissions.DomainOfInfluenceHierarchy.Update,

            Permissions.DomainOfInfluence.Create,
            Permissions.DomainOfInfluence.UpdateAll,
            Permissions.DomainOfInfluence.Update,
            Permissions.DomainOfInfluence.Delete,
            Permissions.DomainOfInfluence.Read,
            Permissions.DomainOfInfluence.ReadAll,

            Permissions.CountingCircle.Read,
            Permissions.CountingCircle.ReadAll,
            Permissions.CountingCircle.Create,
            Permissions.CountingCircle.Update,
            Permissions.CountingCircle.UpdateAll,
            Permissions.CountingCircle.Delete,
            Permissions.CountingCircle.Merge,

            Permissions.Contest.Create,
            Permissions.Contest.Update,
            Permissions.Contest.Delete,
            Permissions.Contest.Read,
            Permissions.Contest.ReadAll,

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

            Permissions.ElectionGroup.Update,
            Permissions.ElectionGroup.Read,
            Permissions.ElectionGroup.ReadAll,

            Permissions.EventLog.Read,
            Permissions.EventLog.ReadAll,

            Permissions.Import.ImportData,

            Permissions.Export.ExportData,
            Permissions.Export.ExportAllPoliticalBusinesses,
        };
        _permissionsPerRole[Roles.ElectionAdmin] = new HashSet<string>
        {
            Permissions.CantonSettings.Read,

            Permissions.DomainOfInfluenceLogo.Update,
            Permissions.DomainOfInfluenceLogo.Read,
            Permissions.DomainOfInfluenceLogo.Delete,

            Permissions.DomainOfInfluence.Update,
            Permissions.DomainOfInfluence.Read,

            Permissions.DomainOfInfluenceHierarchy.Read,

            Permissions.CountingCircle.Read,
            Permissions.CountingCircle.Update,

            Permissions.Contest.Create,
            Permissions.Contest.Update,
            Permissions.Contest.Delete,
            Permissions.Contest.Read,

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

            Permissions.ElectionGroup.Update,
            Permissions.ElectionGroup.Read,

            Permissions.EventLog.Read,

            Permissions.Import.ImportData,

            Permissions.Export.ExportData,
        };
        _permissionsPerRole[Roles.ApiReader] = new HashSet<string>
        {
            Permissions.DomainOfInfluenceHierarchy.ReadAll,
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
