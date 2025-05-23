using Evently.Kafka.Builders;
using Evently.Kafka.Configurations;

namespace Evently.Kafka.Extensions;

public static class ConfigurationExtensions
{
    public static ConsumerBuilder WithConsumerGroup(this Configuration configuration, string groupName)
    {
        configuration.GroupId = groupName;
        return new ConsumerBuilder(configuration, configuration.Context);
    }

    public static ProducerBuilder WithProducers(this Configuration configuration)
    {
        return new ProducerBuilder(configuration);
    }
}