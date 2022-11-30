// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Text.Json.Serialization;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Registration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Voting.Basis.Core.Configuration;
using Voting.Basis.Core.Mapping;
using Voting.Basis.Core.Messaging.Consumers;
using Voting.Basis.Data;
using Voting.Basis.Services;
using Voting.Lib.Common.DependencyInjection;
using Voting.Lib.Grpc.Interceptors;
using Voting.Lib.Rest.Middleware;
using Voting.Lib.Rest.Swagger.DependencyInjection;
using ExceptionHandler = Voting.Basis.Middlewares.ExceptionHandler;
using ExceptionInterceptor = Voting.Basis.Interceptors.ExceptionInterceptor;

namespace Voting.Basis;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly AppConfig _appConfig;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
        _appConfig = configuration.Get<AppConfig>();
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_appConfig);
        services.AddCertificatePinning(_appConfig.CertificatePinning);
        ConfigureHealthChecks(services.AddHealthChecks());

        services.AddAutoMapper(typeof(Startup), typeof(ConverterProfile), typeof(DataContext));
        AddMessaging(services);

        services.AddCore(_appConfig);
        services.AddData(_appConfig.Database, ConfigureDatabase);
        services.AddVotingLibPrometheusAdapter(new() { Interval = _appConfig.PrometheusAdapterInterval });

        ConfigureAuthentication(services.AddVotingLibIam(new() { BaseUrl = _appConfig.SecureConnectApi }));

        if (_appConfig.PublisherModeEnabled)
        {
            AddPublisherServices(services);
        }
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        if (_appConfig.PublisherModeEnabled)
        {
            UsePublisher(app);
        }

        app.UseEndpoints(endpoints =>
        {
            // Metrics and health checks are always exposed, regardless of service mode
            endpoints.MapMetrics();
            endpoints.MapVotingHealthChecks(_appConfig.LowPriorityHealthCheckNames);

            if (_appConfig.PublisherModeEnabled)
            {
                MapEndpoints(endpoints);
            }
        });
    }

    protected virtual void ConfigureAuthentication(AuthenticationBuilder builder)
    {
        builder.AddSecureConnectScheme(options =>
        {
            options.Audience = _appConfig.SecureConnect.Audience;
            options.Authority = _appConfig.SecureConnect.Authority;
            options.ServiceAccount = _appConfig.SecureConnect.ServiceAccount;
            options.ServiceAccountPassword = _appConfig.SecureConnect.ServiceAccountPassword;
        });
    }

    protected virtual void ConfigureDatabase(DbContextOptionsBuilder db)
    {
        db.UseNpgsql(_appConfig.Database.ConnectionString, options => options.CommandTimeout(_appConfig.Database.CommandTimeout));

        // The warning "The same entity is being tracked as different weak entity types..." pops up very often (especially in tests)
        // The reason is our domain of influence and counting circle snapshotting system, which creates duplicates of entities
        // and "sub-entitities", such as the counting circle contact person.
        // We can safely ignore this warning, since the problem would be noticable (it would create two instances in the database).
        db.ConfigureWarnings(w => w.Ignore(CoreEventId.DuplicateDependentEntityTypeInstanceWarning));

#if DEBUG
        // The warning for the missing query split behavior should throw an exception.
        db.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
#endif
    }

    protected virtual void AddMessaging(IServiceCollection services)
    {
        services.AddVotingLibMessaging(
            _appConfig.RabbitMq,
            ConfigureMessagingBus);
    }

    private void UsePublisher(IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandler>();

        if (_appConfig.Publisher.EnableGrpcWeb)
        {
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
            app.UseCorsFromConfig();
        }

        app.UseHttpMetrics();
        app.UseGrpcMetrics();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<IamLoggingHandler>();
        app.UseSerilogRequestLoggingWithTraceabilityModifiers();
        app.UseSwaggerGenerator();
    }

    private void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapGrpcReflectionService();
        endpoints.MapGrpcService<CountingCircleService>();
        endpoints.MapGrpcService<DomainOfInfluenceService>();
        endpoints.MapGrpcService<ContestService>();
        endpoints.MapGrpcService<VoteService>();
        endpoints.MapGrpcService<ProportionalElectionService>();
        endpoints.MapGrpcService<MajorityElectionService>();
        endpoints.MapGrpcService<ElectionGroupService>();
        endpoints.MapGrpcService<ProportionalElectionUnionService>();
        endpoints.MapGrpcService<MajorityElectionUnionService>();
        endpoints.MapGrpcService<ExportService>();
        endpoints.MapGrpcService<ImportService>();
        endpoints.MapGrpcService<EventLogService>();
        endpoints.MapGrpcService<CantonSettingsService>();
        endpoints.MapGrpcService<AdminManagementService>();
    }

    private void ConfigureHealthChecks(IHealthChecksBuilder checks)
    {
        checks
            .AddDbContextCheck<DataContext>()
            .AddEventStore()
            .ForwardToPrometheus();

        if (_appConfig.PublisherModeEnabled && _appConfig.Publisher.EventSignature.Enabled)
        {
            checks.AddEventStoreTransientSubscriptionCatchUp();
            checks.AddPkcs11HealthCheck();
        }
    }

    private void ConfigureMessagingBus(IServiceCollectionBusConfigurator bus)
    {
        // Only Publisher instances need to consume messages from the messaging bus.
        // EventProcessor instances already have the up-to-date data and do not need to receive them via message bus.
        if (!_appConfig.PublisherModeEnabled)
        {
            return;
        }

        bus.AddConsumer<ContestOverviewChangeMessageConsumer>().Endpoint(ConfigureMessagingConsumerEndpoint);
        bus.AddConsumer<ContestDetailsChangeMessageConsumer>().Endpoint(ConfigureMessagingConsumerEndpoint);
    }

    private void ConfigureMessagingConsumerEndpoint(IConsumerEndpointRegistrationConfigurator config)
    {
        config.InstanceId = Environment.MachineName;
        config.Temporary = true;
    }

    private IServiceCollection AddPublisherServices(IServiceCollection services)
    {
        services.AddSingleton(_appConfig.Publisher);
        services.AddEch(_appConfig.Publisher.Ech);
        services
            .AddControllers()
            .AddJsonOptions(x =>
            {
                x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddGrpc(o =>
        {
            o.EnableDetailedErrors = _appConfig.Publisher.EnableDetailedErrors;
            o.Interceptors.Add<ExceptionInterceptor>();
            o.Interceptors.Add<RequestProtoValidatorInterceptor>();
        });

        if (_appConfig.Publisher.EnableGrpcWeb)
        {
            services.AddCors(_configuration);
        }

        services.AddGrpcReflection();
        services.AddProtoValidators();
        services.AddSwaggerGenerator(_configuration);
        return services;
    }
}
