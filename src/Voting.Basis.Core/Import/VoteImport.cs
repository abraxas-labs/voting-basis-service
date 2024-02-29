// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Import;

public class VoteImport
{
    public Vote Vote { get; set; } = new();
}
