namespace Hazelnut.Husk.SourceGenerator;

public class ArgumentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? LongName { get; set; }
    public string? ShortName { get; set; }
    public int Order { get; set; } = -1;
    public bool IsRequired { get; set; }
    public bool IgnoreCaseLongName { get; set; } = true;
    public bool IgnoreCaseShortName { get; set; }
    public bool IsCollection { get; set; }
    public bool IsEnum { get; set; }
    public bool IsNullable { get; set; }
    public string EnumType { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
}