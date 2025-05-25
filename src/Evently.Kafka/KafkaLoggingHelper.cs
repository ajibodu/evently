using Confluent.Kafka;
using Evently.Core.Configurations;
using Microsoft.Extensions.Logging;

namespace Evently.Kafka;

public static class KafkaLoggingHelper
{
    private static readonly ILogger Logger = Collection.ResolveLogger(typeof(KafkaLoggingHelper));
    public static LogLevel LogLevelFromKafka(SyslogLevel level) =>
        level switch
        {
            SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogLevel.Critical,
            SyslogLevel.Error => LogLevel.Error,
            SyslogLevel.Warning => LogLevel.Warning,
            SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
            SyslogLevel.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

    public static void LogKafkaMessage(LogMessage message)
    {
        var level = LogLevelFromKafka(message.Level);
        Logger.Log(level, "Kafka Message: {Name} {Facility} {Message}", message.Name, message.Facility, message.Message);
    }

    public static void LogKafkaError(Error error)
    {
        Logger.LogError("Kafka Error: {Reason} (Code: {Code}, IsBrokerError: {IsBrokerError}, IsLocalError: {IsLocalError}, IsFatal: {IsFatal}, IsError: {IsError})",
            error.Reason,
            error.Code,
            error.IsBrokerError,
            error.IsLocalError,
            error.IsFatal,
            error.IsError);
    }
}