using Evently.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Core.Context;

public class EventlyContext(IServiceCollection services) : IEventlyContext
{
    private readonly Dictionary<Type, List<object>> _configurations = new();
    
    public void RegisterConfiguration<T>(T config) where T : class, IBaseConsumerConfiguration
    {
        var type = typeof(T);
        if (!_configurations.ContainsKey(type))
        {
            _configurations[type] = [];
        }

        _configurations[type].Add(config);
        services.AddSingleton(config);
    }

    public IEnumerable<T> GetBrokerConfiguration<T>() where T : class, IBaseConsumerConfiguration
    {
        var type = typeof(T);
        if (_configurations.TryGetValue(type, out var list))
        {
            return list.Cast<T>();
        }
        throw new InvalidOperationException($"Configuration for {typeof(T).Name} not registered");
    }

    public bool TryGetBrokerConfiguration<T>(out IEnumerable<T> config) where T : class, IBaseConsumerConfiguration
    {
        if (_configurations.TryGetValue(typeof(T), out var list))
        {
            config = list.Cast<T>();
            return true;
        }
        config = new List<T>();
        return false;
    }
}