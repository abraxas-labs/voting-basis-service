// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Ech.Converters;
using Voting.Lib.Ech.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEch(this IServiceCollection services, EchConfig config)
    {
        return services
            .AddVotingLibEch(config)
            .AddSingleton<Ech157Serializer>()
            .AddSingleton<Ech157Deserializer>()
            .AddSingleton<Ech159Serializer>()
            .AddSingleton<Ech159Deserializer>();
    }
}
