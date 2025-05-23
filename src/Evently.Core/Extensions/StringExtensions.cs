using System.Text.RegularExpressions;

namespace Evently.Core.Extensions;

public static class StringExtensions
{
    public static string ToDlQ(this string str)
    {
        return $"{str}-DLQ";
    }
    
    public static string ToKebabCase(this string input)
    {
        return string.IsNullOrEmpty(input) ? input : Regex.Match(input, "^-+")?.ToString() + Regex.Replace(input, "([a-z0-9])([A-Z])", "$1-$2").ToLower();
    }
}