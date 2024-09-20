// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Voting.Basis.Data.Configuration;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Data.Repositories.Snapshot;
using Voting.Lib.Database.Interceptors;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(
        this IServiceCollection services,
        DataConfig config,
        Action<DbContextOptionsBuilder> optionsBuilder)
    {
        services.AddDbContext<DataContext>((serviceProvider, db) =>
        {
            if (config.EnableDetailedErrors)
            {
                db.EnableDetailedErrors();
            }

            if (config.EnableSensitiveDataLogging)
            {
                db.EnableSensitiveDataLogging();
            }

            if (config.EnableMonitoring)
            {
                db.AddInterceptors(serviceProvider.GetRequiredService<DatabaseQueryMonitoringInterceptor>());
            }

            db.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            optionsBuilder(db);
        });

        if (config.EnableMonitoring)
        {
            services.AddDataMonitoring(config.Monitoring);
        }

        return services
            .AddTransient<DomainOfInfluenceCountingCircleRepo>()
            .AddTransient<DomainOfInfluencePermissionRepo>()
            .AddTransient<DomainOfInfluenceHierarchyRepo>()
            .AddTransient<BallotQuestionRepo>()
            .AddTransient<TieBreakQuestionRepo>()
            .AddTransient<ProportionalElectionListUnionEntryRepo>()
            .AddTransient<MajorityElectionBallotGroupEntryRepo>()
            .AddTransient<ProportionalElectionUnionEntryRepo>()
            .AddTransient<MajorityElectionUnionEntryRepo>()
            .AddTransient<ProportionalElectionUnionListRepo>()
            .AddTransient<ProportionalElectionListRepo>()
            .AddTransient<SimplePoliticalBusinessRepo>()
            .AddTransient(typeof(HasSnapshotDbRepository<,>))
            .AddTransient<DomainOfInfluenceCountingCircleSnapshotRepo>()
            .AddTransient<CountingCircleRepo>()
            .AddTransient<CantonSettingsRepo>()
            .AddTransient<DomainOfInfluenceRepo>()
            .AddVotingLibDatabase<DataContext>();
    }
}
