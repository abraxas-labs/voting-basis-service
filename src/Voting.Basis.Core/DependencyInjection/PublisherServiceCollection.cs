// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.EventSignature;
using Voting.Basis.Core.Export;
using Voting.Basis.Core.Export.Generators;
using Voting.Basis.Core.Import;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Core.ObjectStorage;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Core.Services.Read.Snapshot;
using Voting.Basis.Core.Services.Validation;
using Voting.Basis.Core.Services.Write;
using Voting.Basis.Core.Utils;
using Voting.Basis.Core.Validation;
using Voting.Lib.Eventing;
using Voting.Lib.Eventing.DependencyInjection;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Scheduler;

namespace Microsoft.Extensions.DependencyInjection;

internal static class PublisherServiceCollection
{
    internal static IServiceCollection AddPublisherServices(this IServiceCollection services, AppConfig config)
    {
        if (!config.PublisherModeEnabled)
        {
            return services;
        }

        return services
            .AddScoped<PermissionService>()
            .AddSingleton<IPermissionProvider, PermissionProvider>()
            .AddObjectStorage(config.Publisher)
            .AddCryptography(config.Publisher)
            .AddWriterServices(config.Publisher)
            .AddReaderServices();
    }

    internal static IEventingServiceCollection AddPublisher(this IEventingServiceCollection services, AppConfig config)
    {
        if (config.PublisherModeEnabled)
        {
            services.AddPublishing<ContestAggregate>();

            if (config.Publisher.EventSignature.Enabled)
            {
                services.AddTransientSubscription<ContestAggregate>(WellKnownStreams.All);
            }
        }

        return services;
    }

    private static IServiceCollection AddObjectStorage(this IServiceCollection services, PublisherConfig config)
    {
        services
            .AddVotingLibObjectStorage(config.ObjectStorage)
            .AddBucketClient<DomainOfInfluenceLogoStorage>();
        return services;
    }

    private static IServiceCollection AddWriterServices(this IServiceCollection services, PublisherConfig config)
    {
        return services
            .AddScoped<ImportService>()
            .AddScoped<ProportionalElectionListsAndCandidatesImportService>()
            .AddScoped<MajorityElectionCandidatesImportService>()
            .AddScoped<EventInfoProvider>()
            .AddScoped<ContestValidationService>()
            .AddScoped<PoliticalBusinessValidationService>()
            .AddScoped<CountingCircleWriter>()
            .AddScoped<DomainOfInfluenceWriter>()
            .AddScoped<ContestWriter>()
            .AddScoped<VoteWriter>()
            .AddScoped<ProportionalElectionWriter>()
            .AddScoped<MajorityElectionWriter>()
            .AddScoped<ProportionalElectionUnionWriter>()
            .AddScoped<MajorityElectionUnionWriter>()
            .AddScoped<CantonSettingsWriter>()
            .AddScoped<ContestMerger>()
            .AddScoped<EventSignatureWriter>()
            .AddScoped<PoliticalAssemblyWriter>()
            .AddScoped<IAggregateRepositoryHandler, EventSignatureAggregateRepositoryHandler>()
            .AddValidatorsFromAssemblyContaining<EntityOrdersValidator>()
            .AddScheduledJobs(config);
    }

    private static IServiceCollection AddReaderServices(this IServiceCollection services)
    {
        return services
            .AddExports()
            .AddScoped<MajorityElectionUnionReader>()
            .AddScoped<DomainOfInfluenceSnapshotReader>()
            .AddScoped<CountingCircleSnapshotReader>()
            .AddScoped<CantonSettingsReader>()
            .AddScoped<CountingCircleReader>()
            .AddScoped<DomainOfInfluenceReader>()
            .AddScoped<ContestReader>()
            .AddScoped<VoteReader>()
            .AddScoped<ProportionalElectionReader>()
            .AddScoped<MajorityElectionReader>()
            .AddScoped<ProportionalElectionUnionReader>()
            .AddScoped<EventLogReader>()
            .AddScoped<PoliticalAssemblyReader>();
    }

    private static IServiceCollection AddExports(this IServiceCollection services)
    {
        return services.AddScoped<ExportService>()
            .Scan(scan => scan.FromAssemblyOf<IExportGenerator>()
                .AddClasses(classes => classes.AssignableTo<IExportGenerator>())
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo<IExportsGenerator>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());
    }

    private static IServiceCollection AddScheduledJobs(this IServiceCollection services, PublisherConfig config)
    {
        return services
            .AddScheduledJob<EndContestTestingPhaseJob>(config.ContestStateEndTestingPhaseJob)
            .AddScheduledJob<PastLockedContestJob>(config.ContestStateSetPastJob)
            .AddScheduledJob<ArchiveContestJob>(config.ContestStateArchiveJob)
            .AddScheduledJob<ActivateCountingCirclesMergeJob>(config.ActivateCountingCirclesMergeJob)
            .AddScheduledJob<ActivateCountingCircleEVotingJob>(config.ActivateCountingCircleEVotingJob)
            .AddScheduledJob<StopContestEventSignatureJob>(config.StopContestEventSignatureJob);
    }

    private static IServiceCollection AddCryptography(this IServiceCollection services, PublisherConfig config)
    {
        if (config.EnablePkcs11Mock)
        {
            services.AddVotingLibPkcs11Mock();
        }
        else
        {
            services.AddVotingLibPkcs11(config.Pkcs11);
        }

        return services
            .AddVotingLibCryptography()
            .AddEventSignature()
            .AddSingleton(config.EventSignature)
            .AddSingleton(config.Machine)
            .AddSingleton<EventSignatureService>();
    }
}
