// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using Voting.Lib.Testing.Utils;

namespace Voting.Basis.Test;

public static class RawSnapshots
{
    public static void MatchXmlSnapshot(this string xml, params string[] pathSegments)
    {
        xml = XmlUtil.FormatTestXml(xml);
        var path = Path.Join(TestSourcePaths.TestProjectSourceDirectory, Path.Join(pathSegments));

#if UPDATE_SNAPSHOTS
        var updateSnapshot = true;
#else
        var updateSnapshot = false;
#endif
        xml.MatchRawSnapshot(path, updateSnapshot);
    }
}
