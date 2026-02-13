using Axom.Compiler;

public class CodegenRangeBuiltinTests
{
    [Fact]
    public void Compile_range_emits_enumerable_range()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print range(1, 5)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("System.Linq.Enumerable.Range", result.GeneratedCode);
        Assert.Contains("Math.Max(0", result.GeneratedCode);
    }
}
