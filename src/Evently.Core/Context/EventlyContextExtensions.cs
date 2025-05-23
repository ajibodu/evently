using Microsoft.Extensions.DependencyInjection;

namespace Evently.Core.Context;

public static class EventlyContextExtensions
{
    public static IServiceCollection AddEventlyContext(this IServiceCollection services)
    {
        var context = new EventlyContext(services);
        services.AddSingleton<IEventlyContext>(context);
        
        // Register core services
        services.AddOptions();
        
        return services;
    }
    
    public static EventlyContext GetEventlyContext(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IEventlyContext));
        if (descriptor?.ImplementationInstance is EventlyContext context)
        {
            return context;
        }
        
        throw new InvalidOperationException("Evently context not found. Call services.AddEvently() first.");
    }
}