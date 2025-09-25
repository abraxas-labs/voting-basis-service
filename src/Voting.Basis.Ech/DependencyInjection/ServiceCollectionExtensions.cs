// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Ech.Converters;
using Voting.Basis.Ech.Converters.V4;
using Voting.Lib.Ech.Configuration;
using v5 = Voting.Basis.Ech.Converters.V5;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEch(this IServiceCollection services, EchConfig config)
    {
        return services
            .AddVotingLibEch(config)
            .AddSingleton<Ech0157Deserializer>()
            .AddSingleton<Ech0159Deserializer>()
            .AddSingleton<Ech0157Serializer>()
            .AddSingleton<Ech0159Serializer>()
            .AddSingleton<v5.Ech0157Serializer>()
            .AddSingleton<v5.Ech0159Serializer>()
            .AddSingleton<EchSerializerProvider>();
    }
}
