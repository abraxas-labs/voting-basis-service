// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Services.Write;

public abstract class PoliticalBusinessWriter
{
    public abstract PoliticalBusinessType Type { get; }

    /// <summary>
    /// Delete the political businesses.
    /// Should not perform a permission check.
    /// </summary>
    /// <param name="ids">The ids to delete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal abstract Task DeleteWithoutChecks(List<Guid> ids);
}
