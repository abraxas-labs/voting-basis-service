// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.EventProcessors;
using Voting.Basis.Core.Utils;
using Voting.Lib.Eventing;
using Voting.Lib.Eventing.DependencyInjection;

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
            .AddScoped<DomainOfInfluenceCantonDefaultsBuilder>()
            .AddScoped<DomainOfInfluenceHierarchyBuilder>()
            .AddScoped<ContestCountingCircleOptionsReplacer>()
            .AddScoped<DomainOfInfluenceCountingCircleInheritanceBuilder>()
            .AddScoped(typeof(SimplePoliticalBusinessBuilder<>))
            .AddScoped<EventLogger>()
            .AddScoped<EventLoggerAdapter>();
    }

    internal static IEventingServiceCollection AddEventProcessors(this IEventingServiceCollection services, AppConfig config)
    {
        return config.EventProcessorModeEnabled
            ? services.AddSubscription<EventProcessorScope>(WellKnownStreams.CategoryVoting)
            : services;
    }
}
