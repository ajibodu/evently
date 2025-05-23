using Evently.Core.Extensions;

namespace Evently.Core;

public interface INameFormater
{
    string Format<T>();
    string Format(Type type);
}

public class FullNameFormater : INameFormater
{
    public string Format<T>()
    {
        return Format(typeof(T));
    }

    public string Format(Type type)
    {
        var fullName = type.FullName;
        ArgumentException.ThrowIfNullOrEmpty(fullName);
        return fullName;
    }
}

public class NameFormater : INameFormater
{
    public string Format<T>()
    {
        return Format(typeof(T));
    }
    
    public string Format(Type type)
    {
        var name = type.Name;
        ArgumentException.ThrowIfNullOrEmpty(name);
        return name;
    }
}

public class KebabCaseNameFormater : INameFormater
{
    public string Format<T>()
    {
        return Format(typeof(T));
    }
    
    public string Format(Type type)
    {
        return type.Name.ToKebabCase();
    }
}

public class SnakeCaseNameFormater : INameFormater
{
    public string Format<T>()
    {
        return Format(typeof(T));
    }
    
    public string Format(Type type)
    {
        return new KebabCaseNameFormater().Format(type).Replace('-', '_');
    }
}