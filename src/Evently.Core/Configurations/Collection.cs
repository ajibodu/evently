namespace Evently.Core.Configurations;

public static class Collection
{
    public static Func<INameFormater> NameFormaterResolver { get; set; } = () => new KebabCaseNameFormater();

    public static void SetNameFormater(INameFormater nameFormater)
    {
        NameFormaterResolver = () => nameFormater;
    }
}