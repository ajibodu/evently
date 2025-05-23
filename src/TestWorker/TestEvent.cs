using Evently.Core;

namespace TestWorker;

public class TestEvent
{
    public string Name { get; set; }
}

public class TestConsumer(ILogger<TestConsumer> logger) : IConsumer<TestEvent>
{
    public Task HandleAsync(ConsumerContext<TestEvent> context, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{GetType()} - {context.ConsumerName} - {context.Message.Name}");
        
        //throw new Exception($"Something bad happened at {GetType()} - {context.ConsumerName}");
        
        return Task.CompletedTask;
    }
}

public class AnotherEvent
{
    public string Name { get; set; }
}
public class AnotherEventConsumer(ILogger<TestConsumer> logger) : IConsumer<AnotherEvent>
{
    public Task HandleAsync(ConsumerContext<AnotherEvent> context, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{GetType()} - {context.ConsumerName} - {context.Message.Name}");
        
        return Task.CompletedTask;
    }
}