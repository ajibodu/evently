using Evently.Core.HostedServices;
using Microsoft.Extensions.Logging;

namespace Evently.Redis.HostedServices;

public class RedisConsumerService(
    IServiceProvider serviceProvider, 
    Configurations.Configuration config, 
    IRedisMessenger messenger, 
    ILogger<RedisConsumerService> logger) : BaseConsumerService(serviceProvider, config, messenger, logger)
{
    
}