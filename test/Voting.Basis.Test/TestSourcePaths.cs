// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using Voting.Lib.Testing.Utils;

namespace Voting.Basis.Test;

public static class TestSourcePaths
{
    public static readonly string TestProjectSourceDirectory = Path.Join(
        ProjectUtil.FindSolutionDirectory(),
        "test",
        "Voting.Basis.Test");
}
