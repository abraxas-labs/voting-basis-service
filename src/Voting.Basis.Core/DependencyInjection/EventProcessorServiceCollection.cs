// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.Extensions.DependencyInjection.Extensions;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.EventProcessors;
using Voting.Basis.Core.Utils;
using Voting.Lib.Eventing;
using Voting.Lib.Eventing.DependencyInjection;
using Voting.Lib.Eventing.Subscribe;

namespace Microsoft.Extensions.DependencyInjection;

internal static class EventProcessorServiceCollection
{
    internal static IServiceCollection AddEventProcessorServices(this IServiceCollection services, AppConfig config)
    {
        if (!config.EventProcessorModeEnabled)
        {
            return services;
        }

        return services
            .AddScoped<ProportionalElectionUnionListBuilder>()
            .AddScoped<ProportionalElectionListBuilder>()
            .AddScoped<DomainOfInfluenceHierarchyBuilder>()
            .AddScoped<DomainOfInfluenceCountingCircleInheritanceBuilder>()
            .AddScoped(typeof(SimplePoliticalBusinessBuilder<>))
            .AddScoped<EventLogger>()
            .AddScoped<EventLoggerAdapter>();
    }

    internal static IEventingServiceCollection AddEventProcessors(this IEventingServiceCollection services, AppConfig config)
    {
        if (!config.EventProcessorModeEnabled)
        {
            return services;
        }

        services.Services.AddSingleton<EventProcessingInMemoryStateHolder>();
        services.Services.TryAddSingleton<IEventProcessingRetryPolicy<EventProcessorScope>, EventProcessingRetryPolicy>();
        return services.AddSubscription<EventProcessorScope>(WellKnownStreams.CategoryVoting);
    }
}
