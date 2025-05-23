using Evently.Core.HostedServices;
using Microsoft.Extensions.Logging;

namespace Evently.Azure.ServiceBus.HostedServices;

public class ConsumerService(
    IServiceProvider serviceProvider, 
    Configurations.Configuration config, 
    IServiceBusMessenger messenger, 
    ILogger<ConsumerService> logger) : BaseConsumerService(serviceProvider, config, messenger, logger)
{
    
}