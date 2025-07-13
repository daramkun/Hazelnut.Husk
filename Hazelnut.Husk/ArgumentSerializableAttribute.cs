namespace Hazelnut.Husk;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class ArgumentSerializableAttribute(bool generateParserSource = true) : Attribute
{
    public bool GenerateParserSource { get; } = generateParserSource;
}