// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string ElectionAdmin = "Wahlverwalter";
    public const string ApiReader = "ApiReader";

    public static bool IsAdmin(this IAuth auth) => auth.HasRole(Admin);

    public static void EnsureAdmin(this IAuth auth) => auth.EnsureRole(Admin);

    public static void EnsureAdminOrElectionAdmin(this IAuth auth) => auth.EnsureAnyRole(Admin, ElectionAdmin);

    public static void EnsureApiReader(this IAuth auth) => auth.EnsureRole(ApiReader);
}
