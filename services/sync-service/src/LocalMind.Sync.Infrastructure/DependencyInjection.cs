namespace LocalMind.Sync.Infrastructure;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Infrastructure.Mongo;
using LocalMind.Sync.Infrastructure.Options;
using LocalMind.Sync.Infrastructure.Queues;
using LocalMind.Sync.Infrastructure.Redis;
using LocalMind.Sync.Infrastructure.Time;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSyncInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.Configure<MongoSyncOptions>(configuration.GetSection(MongoSyncOptions.SectionName));
        services.Configure<RedisSyncOptions>(configuration.GetSection(RedisSyncOptions.SectionName));
        services.Configure<RabbitMqSyncOptions>(configuration.GetSection(RabbitMqSyncOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<MongoSyncContext>();
        services.AddSingleton<RedisConnectionFactory>();

        services.AddScoped<IDeviceRepository, MongoDeviceRepository>();
        services.AddScoped<ISyncSessionRepository, MongoSyncSessionRepository>();
        services.AddScoped<IManifestRepository, MongoManifestRepository>();
        services.AddScoped<IChangeRepository, MongoChangeRepository>();
        services.AddScoped<IConflictRepository, MongoConflictRepository>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
        services.AddScoped<ISyncQueuePublisher, MassTransitSyncQueuePublisher>();
        services.AddMassTransit(configurator =>
        {
            configurator.SetKebabCaseEndpointNameFormatter();
            configureConsumers?.Invoke(configurator);

            configurator.UsingRabbitMq((context, rabbit) =>
            {
                RabbitMqSyncOptions options = configuration.GetSection(RabbitMqSyncOptions.SectionName).Get<RabbitMqSyncOptions>() ?? new RabbitMqSyncOptions();
                rabbit.Host(new Uri($"rabbitmq://{options.HostName}:{options.Port}/"), host =>
                {
                    host.Username(options.UserName);
                    host.Password(options.Password);
                });

                rabbit.UseMessageRetry(retry => retry.Exponential(3, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200)));
                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
