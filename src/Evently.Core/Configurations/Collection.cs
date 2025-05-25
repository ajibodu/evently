using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Evently.Core.Configurations;

public static class Collection
{
    public static Func<INameFormater> NameFormaterResolver { get; set; } = () => new KebabCaseNameFormater();
    public static Func<Type, ILogger> ResolveLogger { get; private set; } = _ => NullLogger.Instance;


    public static void SetNameFormater(INameFormater nameFormater)
    {
        NameFormaterResolver = () => nameFormater;
    }
    
    public static void SetLoggerFactory(ILoggerFactory loggerFactory)
    {
        ResolveLogger = loggerFactory.CreateLogger;
    }
}