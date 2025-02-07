// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;

namespace Voting.Basis.Test.ImportTests.TestFiles;

public static class EchTestFiles
{
    public const string Ech0157FileName = "eCH0157_both_election_types.xml";
    public const string Ech0159FileName = "eCH0159.xml";
    public const string Ech0159AllTypesFileName = "eCH0159_all_types.xml";
    public const string Ech0159InvalidFileName = "eCH0159_invalid.xml";

    public static string GetTestFilePath(string fileName)
    {
        var assemblyFolder = Path.GetDirectoryName(typeof(EchTestFiles).Assembly.Location);
        return Path.Join(assemblyFolder, $"ImportTests/TestFiles/{fileName}");
    }
}
