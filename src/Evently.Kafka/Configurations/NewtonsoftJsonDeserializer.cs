using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace Evently.Kafka.Configurations;

public class NewtonsoftJsonDeserializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json)!;
    }
}