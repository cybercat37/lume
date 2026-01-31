using Lume.Compiler;

public class CodegenGroupingTests
{
    [Fact]
    public void Compile_nested_grouping_preserves_parentheses()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print (1 + 2) * (3 + 4)", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("(1 + 2) * (3 + 4)", result.GeneratedCode);
    }

    [Fact]
    public void Compile_unary_minus_with_multiplication()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print -1 * 2", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("-1 * 2", result.GeneratedCode);
    }

    [Fact]
    public void Compile_complex_precedence_chain()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1 + 2 * 3 - 4", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("1 + 2 * 3 - 4", result.GeneratedCode);
    }

    [Fact]
    public void Compile_nested_parentheses_deep()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print ((1 + 2) * 3) + 4", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("(1 + 2) * 3 + 4", result.GeneratedCode);
    }

    [Fact]
    public void Compile_division_and_subtraction_chain()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 10 / 2 - 3 * 4", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("10 / 2 - 3 * 4", result.GeneratedCode);
    }
}
