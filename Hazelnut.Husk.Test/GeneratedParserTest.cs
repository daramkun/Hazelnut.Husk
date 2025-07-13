namespace Hazelnut.Husk.Test;

[TestClass]
public class GeneratedParserTest
{
    [TestMethod]
    public void Test1()
    {
        var args = new[] {"--string-argument", "test", "--bool-argument", "--integer-argument", "123"};
        var result = new TestClass1(args);
        
        Assert.AreEqual("test", result.StringArgument);
        Assert.AreEqual(true, result.BoolArgument);
        Assert.AreEqual(123, result.IntegerArgument);
    }
    
    [TestMethod]
    public void Test2()
    {
        var args = new[] {"--string-argument=test", "--bool-argument=false", "--integer-argument=123"};
        var result = new TestClass1(args);
        
        Assert.AreEqual("test", result.StringArgument);
        Assert.AreEqual(false, result.BoolArgument);
        Assert.AreEqual(123, result.IntegerArgument);
    }

    [TestMethod, ExpectedException(typeof(ArgumentException))]
    public void Test3()
    {
        var args = new[] {"--bool-argument=false", "--integer-argument=123"};
        var result = new TestClass1(args);
    }
}