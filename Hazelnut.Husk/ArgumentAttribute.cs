namespace Hazelnut.Husk;

[Serializable]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ArgumentAttribute : Attribute
{
    public string? LongName { get; set; }
    public string? ShortName { get; set; }

    public int Order { get; set; } = -1;
    
    public bool IsRequired { get; set; }

    public bool IgnoreCaseLongName { get; set; } = true;
    public bool IgnoreCaseShortName { get; set; } = false;

    public bool IsNameEquals(string arg)
    {
        if (arg.StartsWith("--"))
        {
            return arg.Equals("--" + LongName, IgnoreCaseLongName
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
        }
        
        if (arg.StartsWith('-'))
        {
            return arg.Equals('-' + ShortName, IgnoreCaseShortName
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
        }

        return false;
    }
}