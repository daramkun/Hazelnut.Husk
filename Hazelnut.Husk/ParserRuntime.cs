using System.Text.RegularExpressions;

namespace Hazelnut.Husk;

public static class ParserRuntime
{
    public static bool ParseBoolean(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "1" or "yes" or "y" or "on" or "t" or "true" => true,
            "0" or "no" or "n" or "off" or "f" or "false" => false,
            _ => throw new ArgumentException($"Invalid boolean value: {value}")
        };
    }

    public static DateTime ParseDateTime(string value) => DateTime.Parse(value);
    public static TimeSpan ParseTimeSpan(string value) => TimeSpan.Parse(value);
    public static Regex ParseRegex(string value) => new(value);
    public static Uri ParseUri(string value) => new(value, UriKind.RelativeOrAbsolute);
}