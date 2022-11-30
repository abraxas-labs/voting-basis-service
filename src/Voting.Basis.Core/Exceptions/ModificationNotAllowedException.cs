// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class ModificationNotAllowedException : Exception
{
    public ModificationNotAllowedException()
        : base("Some modifications are not allowed because the testing phase has ended.")
    {
    }
}
