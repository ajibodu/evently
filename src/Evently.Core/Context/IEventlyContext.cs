using Evently.Core.Configurations;

namespace Evently.Core.Context;

public interface IEventlyContext
{
    void RegisterConfiguration<T>(T config) where T : class, IBaseConsumerConfiguration;
    IEnumerable<T> GetBrokerConfiguration<T>() where T : class, IBaseConsumerConfiguration;
    bool TryGetBrokerConfiguration<T>(out IEnumerable<T> config) where T : class, IBaseConsumerConfiguration;
}
