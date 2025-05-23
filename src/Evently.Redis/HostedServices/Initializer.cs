using Microsoft.Extensions.Hosting;

namespace Evently.Redis.HostedServices;

public class Initializer(IRedisStreamManager streamManager, Configurations.Configuration config) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await streamManager.RegisterQueuesAsync(config.RedisConsumerConfiguration);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}