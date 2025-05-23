namespace Evently.Core;

public record ConsumerContext<TEvent>(TEvent Message, string ConsumerName);