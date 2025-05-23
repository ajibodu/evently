using Evently.Core.HostedServices;
using Microsoft.Extensions.Logging;

namespace Evently.Kafka.HostedServices;

public class ConsumerService(
    IServiceProvider serviceProvider, 
    Configurations.Configuration config, 
    IKafkaMessenger messenger, 
    ILogger<ConsumerService> logger) 
    : BaseConsumerService(serviceProvider, config, messenger, logger)
{
    
}