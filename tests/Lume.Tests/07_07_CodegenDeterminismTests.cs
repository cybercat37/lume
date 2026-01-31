using Lume.Compiler;

public class CodegenDeterminismTests
{
    [Fact]
    public void Compile_same_input_is_deterministic()
    {
        var compiler = new CompilerDriver();
        var result1 = compiler.Compile("print 1\nprint 2", "test.lume");
        var result2 = compiler.Compile("print 1\nprint 2", "test.lume");

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.GeneratedCode, result2.GeneratedCode);
    }
}
