// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Data.Extensions;
public static class PoliticalAssemblyStateExtension
{
    public static bool IsLocked(this PoliticalAssemblyState state) => state is PoliticalAssemblyState.PastLocked or PoliticalAssemblyState.Archived;
}
