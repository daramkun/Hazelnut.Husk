namespace Hazelnut.Husk.Test;

[Serializable, ArgumentSerializable]
internal partial class TestClass1
{
    [Argument(LongName = "string-argument", ShortName = "s", IsRequired = true)]
    public string StringArgument { get; set; } = string.Empty;
    [Argument(LongName = "bool-argument", ShortName = "b")]
    public bool BoolArgument { get; set; }
    [Argument(LongName = "integer-argument", ShortName = "i")]
    public int IntegerArgument { get; set; }
}