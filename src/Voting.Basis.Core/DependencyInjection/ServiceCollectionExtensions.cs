// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Utils;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, AppConfig config)
    {
        return services
            .AddScoped<DomainOfInfluencePermissionBuilder>()
            .AddEventProcessorServices(config)
            .AddPublisherServices(config)
            .AddEventing(config)
            .AddSystemClock();
    }

    private static IServiceCollection AddEventing(this IServiceCollection services, AppConfig config)
    {
        return services.AddVotingLibEventing(config.EventStore, typeof(EventInfo).Assembly)
            .AddEventProcessors(config)
            .AddPublisher(config)
            .Services;
    }
}
