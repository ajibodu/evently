using System.Reflection;
using Evently.Core.Builders;
using Evently.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Core.Configurations;

public class Providers(IServiceCollection serviceCollection)
{
    public ConsumerBuilder AddConsumer<TEvent, TConsumer>(int consumersPerStream = 1, RetryConfiguration? retryConfiguration = null)
        where TEvent : new()
        where TConsumer : IConsumer<TEvent>
    {
        var context = serviceCollection.GetEventlyContext();
        var consumer = new ConsumerBuilder(context);
        consumer.AddConsumer<TEvent, TConsumer>(consumersPerStream, retryConfiguration);
        
        return consumer;
    }
    
    public static void SetNameFormatter(INameFormater nameFormatter)
    {
        Collection.SetNameFormater(nameFormatter);
    }
    
    public Providers AddConsumersFromNamespaceContaining<TConsumerMarker>(int consumersPerStream = 1, RetryConfiguration? retryConfiguration = null)
    {
        if (typeof(TConsumerMarker).Namespace is null)
            throw new InvalidOperationException($"Type {typeof(TConsumerMarker).Name} has no namespace");

        var assembly = typeof(TConsumerMarker).Assembly;
        var targetNamespace = typeof(TConsumerMarker).Namespace;

        var consumerTypes = assembly
            .GetTypes()
            .Where(t => 
                t.Namespace == targetNamespace 
                && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

        foreach (var consumerType in consumerTypes)
        {
            var eventType = consumerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .GetGenericArguments()[0];

            var method = GetType().GetMethod(nameof(AddConsumer));
            if(method == null)
                throw new InvalidOperationException($"Type {consumerType.Name} has no AddConsumer({eventType.FullName})");
                    
            method = method.MakeGenericMethod(eventType, consumerType);
            method.Invoke(this, [consumersPerStream, retryConfiguration]);
        }

        return this;
    }
    
    public Providers AddAllConsumersInAssembly(int consumersPerStream = 1, RetryConfiguration? retryConfiguration = null)
    {
        var assembly = Assembly.GetCallingAssembly();
        var consumerTypes = assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

        foreach (var consumerType in consumerTypes)
        {
            var eventType = consumerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .GetGenericArguments()[0];

            // Create generic method dynamically
            var method = typeof(Providers).GetMethod(nameof(AddConsumer), BindingFlags.Public | BindingFlags.Instance);
            if(method == null)
                throw new InvalidOperationException($"Type {consumerType.Name} has no AddConsumer({eventType.FullName})");
                    
            method = method.MakeGenericMethod(eventType, consumerType);
            method.Invoke(this, [consumersPerStream, retryConfiguration]);
        }

        return this;
    }
}