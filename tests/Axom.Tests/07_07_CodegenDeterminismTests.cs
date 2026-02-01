using Axom.Compiler;

public class CodegenDeterminismTests
{
    [Fact]
    public void Compile_same_input_is_deterministic()
    {
        var compiler = new CompilerDriver();
        var result1 = compiler.Compile("print 1\nprint 2", "test.axom");
        var result2 = compiler.Compile("print 1\nprint 2", "test.axom");

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.GeneratedCode, result2.GeneratedCode);
    }
}
