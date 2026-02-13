using Axom.Compiler;

public class CodegenRangeBuiltinTests
{
    [Fact]
    public void Compile_range_emits_axom_range_helper()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print range(1, 5)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomRange(1, 5)", result.GeneratedCode);
        Assert.Contains("static List<int> AxomRange", result.GeneratedCode);
    }

    [Fact]
    public void Compile_range_with_step_emits_axom_range_call()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print range(1, 10, 2)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomRange(1, 10, 2)", result.GeneratedCode);
    }
}
