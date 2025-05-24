using System.Net.Sockets;
using System.Reflection;
using Evently.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Polly.Wrap;

namespace Evently.Core.HostedServices;

public abstract class BaseConsumerService(
    IServiceProvider serviceProvider,
    BaseBusConfiguration config,
    IMessenger messenger,
    ILogger<BaseConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerTasks = config.GetConsumerConfigurations()
            .Select(registration => RunConsumerUntilCancelled(registration, stoppingToken))
            .ToList();

        await Task.WhenAll(consumerTasks);
    }
    
    private Task RunConsumerUntilCancelled(IBaseConsumerConfiguration configuration, CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting consumer for {ConsumerName}", configuration.ConsumerName);

        var policy = RetryPolicy.Create(configuration, config, logger);
        
        // Use reflection to call generic ConsumeAsync
        var consumeAsync = typeof(IMessenger).GetMethod(nameof(IMessenger.ConsumeAsync))!.MakeGenericMethod(configuration.EventType);
        if (consumeAsync == null)
            throw new InvalidOperationException("ConsumeAsync method not found.");
        
        return Task.Run(async () =>
        {
            try
            {
                Func<object, Task> messageHandler = async (message) => 
                {
                    await ProcessAsync(configuration, message, policy, stoppingToken);
                };

                var consumeTask = (Task)consumeAsync.Invoke(messenger, [configuration.ConsumerName, messageHandler, stoppingToken])!;
                await consumeTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing event {EventName}", configuration.EventName);
            }
        }, stoppingToken);
    }

    private async Task ProcessAsync(IBaseConsumerConfiguration configuration, object message, AsyncPolicy policy, CancellationToken stoppingToken)
    {
        var contextType = typeof(ConsumerContext<>).MakeGenericType(configuration.EventType);
        var context = Activator.CreateInstance(contextType, message, configuration.ConsumerName);

        try
        {
            var pollyContext = new Polly.Context
            {
                ["Identifier"] = $"{configuration.ConsumerName} - {configuration.EventType}"
            };
            
            await policy.ExecuteAsync( async (ctx, cancellationToken) =>
            {
                var interfaceType = typeof(IConsumer<>).MakeGenericType(configuration.EventType);
                using var scope = serviceProvider.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService(interfaceType);
                        
                var handleAsync = consumer.GetType().GetMethod(nameof(IConsumer<object>.HandleAsync))!;
                var handleTask = (Task)handleAsync.Invoke(consumer, [context, cancellationToken])!;
                        
                await handleTask.ConfigureAwait(false);
            }, pollyContext, stoppingToken);
        }
        catch (Exception e)
        {
            var actualEx = e is TargetInvocationException tie ? tie.InnerException : e;
            logger.LogError(actualEx, "Error for {Consumer} processing message {Event} {Message} => Publishing to DLQ", configuration.ConsumerName, configuration.EventType, message);
            throw;
        }
    }

}