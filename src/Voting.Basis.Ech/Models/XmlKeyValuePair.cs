// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Ech.Models;

public class XmlKeyValuePair
{
    public XmlKeyValuePair()
    {
    }

    public XmlKeyValuePair(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
