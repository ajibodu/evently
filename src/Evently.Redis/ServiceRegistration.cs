using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Context;
using Evently.Redis.Configurations;
using Evently.Redis.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Evently.Redis;

public static class ServiceRegistration
{
    public static IServiceCollection WithRedis(this Providers providers, IServiceCollection services, Action<Configuration> config)
    {
        var redisConfig = new Configuration(services.GetEventlyContext());
        config(redisConfig);
        ArgumentNullException.ThrowIfNull(redisConfig.ConnectionString);
        
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();
            var redis = ConnectionMultiplexer.Connect(redisConfig.ConnectionString);
            
            redis.ConnectionFailed += (_, e) => logger.LogWarning(e.Exception, "Redis connection failed");
            redis.ConnectionRestored += (_, e) => logger.LogInformation("Redis connection restored");
            return redis;
        });

        services.AddSingleton<IRedisMessenger, RedisMessenger>();
        
        services.AddSingleton<Configuration>(_ => redisConfig);
            
        if (redisConfig.RedisConsumerConfiguration.Count > 0)
        {
            foreach (var registration in redisConfig.RedisConsumerConfiguration)
                services.AddScoped(typeof(IConsumer<>).MakeGenericType(registration.EventType), registration.ConsumerType);
                
            services.AddSingleton<IRedisStreamManager, RedisStreamManager>();
        
            // Register hosted service to initialize streams
            services.AddHostedService<Initializer>();
            services.AddHostedService<RedisConsumerService>();
        }
        
        return services;
    }
}