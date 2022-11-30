// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Exceptions;

public class MajorityElectionWithExistingSecondaryElectionsException : Exception
{
    internal MajorityElectionWithExistingSecondaryElectionsException()
        : base("Majority election with existing secondary elections cannot be deleted")
    {
    }
}
