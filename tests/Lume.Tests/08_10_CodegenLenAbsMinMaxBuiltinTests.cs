using Lume.Compiler;

public class CodegenLenAbsMinMaxBuiltinTests
{
    [Fact]
    public void Codegen_len_emits_length()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print len(\"hello\")", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("\"hello\".Length", result.GeneratedCode);
    }

    [Fact]
    public void Codegen_abs_emits_math_abs()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print abs(-5)", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Math.Abs(-5)", result.GeneratedCode);
    }

    [Fact]
    public void Codegen_min_emits_math_min()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print min(5, 10)", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Math.Min(5, 10)", result.GeneratedCode);
    }

    [Fact]
    public void Codegen_max_emits_math_max()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print max(5, 10)", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Math.Max(5, 10)", result.GeneratedCode);
    }
}
