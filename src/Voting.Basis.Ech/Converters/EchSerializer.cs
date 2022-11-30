// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Voting.Basis.Ech.Converters;

public static class EchSerializer
{
    private static readonly Encoding _encoding = new UTF8Encoding(false);
    private static readonly XmlWriterSettings _xmlSettings = new XmlWriterSettings
    {
        Indent = false,
        NewLineOnAttributes = false,
        Encoding = _encoding,
    };

    public static byte[] ToXml(object o)
    {
        var serializer = new XmlSerializer(o.GetType());
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream, _encoding);
        using var xmlWriter = XmlWriter.Create(streamWriter, _xmlSettings);
        serializer.Serialize(xmlWriter, o);
        return memoryStream.ToArray();
    }
}
