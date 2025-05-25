using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Evently.Azure.ServiceBus;
using Evently.Azure.ServiceBus.Extensions;
using Evently.Core;
using Evently.Core.Configurations;
using Evently.Kafka;
using Evently.Kafka.Configurations;
using Evently.Kafka.Extensions;
using Evently.Redis;
using Evently.Redis.Extensions;
using TestWorker;

var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

builder.Services.AddEvently((sp, ev) =>
{
    // ev.WithRedis(sp, action =>
    // {
    //     action.ConnectionString = "localhost:6379,abortConnect=false,connectRetry=5,connectTimeout=5000";
    //     action.WithConsumerGroup("evently-redis-consumer-group")
    //         .ConfigureConsumer<TestConsumer>()
    //         .AddConsumer<AnotherEvent, AnotherEventConsumer>();
    // });
    
    var provider = sp.BuildServiceProvider();
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    
    Providers.SetNameFormatter(new KebabCaseNameFormater());
    Providers.SetLoggerFactory(loggerFactory);

    ev
        .AddConsumer<TestEvent, TestConsumer>();
        //.AddConsumer<AnotherEvent, AnotherEventConsumer>();

    //ev.AddConsumersFromNamespaceContaining<TestConsumer>();
    //ev.AddAllConsumersInAssembly();
    
    ev.WithKafka(sp, action =>
    {
        action.BootStrapServers = "10.81.1.156:9092,10.81.1.179:9092,10.81.1.180:9092";
        action.Control = new ConsumerControl()
        {
            SessionTimeoutMs = 6000,
            RetryBackoffMs = 1000,
            MessageSendMaxRetries = 3,
        };
    
        action.WithConsumerGroup("evently-consumer-group")
            .ConfigureConsumer<TestConsumer>(maxConsumer:2, createIfNotExistControl: new CreateIfNotExistControl(5, 3))
            .AddConsumer<AnotherEvent, AnotherEventConsumer>("another-event-consumer", createIfNotExistControl: new CreateIfNotExistControl());
        
        action.WithProducers()
            .AddProducer<string, AnotherEvent>("topic");
        
        action.WithRetry(new RetryConfiguration(RetryStrategy.ExponentialWithJitter, 3, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60)));
    });
    
    // ev.WithAzureServiceBus(sp, action =>
    // {
    //     action.ConnectionString = "10.81.1.156:9092,10.81.1.179:9092,10.81.1.180:9092";
    //     action.ServiceBusProcessorOptions = new ServiceBusProcessorOptions();
    //     action.CreateTopicOrQueueIfNotExist = true;
    //     
    //     action.AddQueueConsumer<TestEvent, TestConsumer>("queue")
    //         .AddTopicConsumer<AnotherEvent, AnotherEventConsumer>("topic", "subscription", ruleOption: f => f
    //             .WithRule("Channel", new SqlRuleFilter(""))
    //             .WithSqlEventTypeRule())
    //         .AddQueueConsumer<TestEvent, TestConsumer>(retryConfiguration: new RetryConfiguration(RetryStrategy.Incremental, 3, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60)));
    //     
    // //:TODO add implementation to setup queue forwader on a tapic (action.MapTopicToQueue<TEvent>("topic", "subscription")
    //
    //     action.WithProducers()
    //         .AddProducer<AnotherEvent>("topic");
    //
    //     action.WithRetry(new RetryConfiguration(RetryStrategy.ExponentialWithJitter, 3, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60)));
    // });
});

var host = builder.Build();
host.Run();