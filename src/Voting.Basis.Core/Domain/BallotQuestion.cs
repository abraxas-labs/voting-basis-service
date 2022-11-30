// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class BallotQuestion
{
    public BallotQuestion()
    {
        Question = new Dictionary<string, string>();
    }

    public int Number { get; private set; }

    public Dictionary<string, string> Question { get; private set; }
}
