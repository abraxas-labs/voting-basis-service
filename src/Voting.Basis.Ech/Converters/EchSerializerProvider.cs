// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Ech0157SerializerV4 = Voting.Basis.Ech.Converters.V4.Ech0157Serializer;
using Ech0157SerializerV5 = Voting.Basis.Ech.Converters.V5.Ech0157Serializer;
using Ech0159SerializerV4 = Voting.Basis.Ech.Converters.V4.Ech0159Serializer;
using Ech0159SerializerV5 = Voting.Basis.Ech.Converters.V5.Ech0159Serializer;

namespace Voting.Basis.Ech.Converters;

public class EchSerializerProvider
{
    private readonly Ech0157SerializerV4 _ech0157SerializerV4;
    private readonly Ech0157SerializerV5 _ech0157SerializerV5;
    private readonly Ech0159SerializerV4 _ech0159SerializerV4;
    private readonly Ech0159SerializerV5 _ech0159SerializerV5;

    public EchSerializerProvider(
        Ech0157SerializerV4 ech0157SerializerV4,
        Ech0157SerializerV5 ech0157SerializerV5,
        Ech0159SerializerV4 ech0159SerializerV4,
        Ech0159SerializerV5 ech0159SerializerV5)
    {
        _ech0157SerializerV4 = ech0157SerializerV4;
        _ech0157SerializerV5 = ech0157SerializerV5;
        _ech0159SerializerV4 = ech0159SerializerV4;
        _ech0159SerializerV5 = ech0159SerializerV5;
    }

    public IEch0157Serializer GetEch0157Serializer(bool v5)
        => v5 ? _ech0157SerializerV5 : _ech0157SerializerV4;

    public IEch0159Serializer GetEch0159Serializer(bool v5)
        => v5 ? _ech0159SerializerV5 : _ech0159SerializerV4;
}
