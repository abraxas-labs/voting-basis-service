// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.EventSignature;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventSignature(this IServiceCollection services)
    {
        return services.AddSingleton<ContestCache>();
    }
}
