using Evently.Redis.Builders;
using Evently.Redis.Configurations;

namespace Evently.Redis.Extensions;

public static class ConfigurationExtensions
{
    public static ConsumerBuilder WithConsumerGroup(this Configuration configuration, string groupName)
    {
        configuration.ConsumerGroup = groupName;
        return new ConsumerBuilder(configuration.Context);
    }
}