// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Google.Protobuf.Collections;

namespace Voting.Basis.Test.ProtoValidatorTests.Utils;

public static class MapFieldUtil
{
    public static void ClearAndAdd(MapField<string, string> field, string key, string value)
    {
        field.Clear();
        field.Add(key, value);
    }
}
