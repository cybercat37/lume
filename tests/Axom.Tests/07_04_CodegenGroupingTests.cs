using Axom.Compiler;

public class CodegenGroupingTests
{
    [Fact]
    public void Compile_nested_grouping_preserves_parentheses()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print (1 + 2) * (3 + 4)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("(1 + 2) * (3 + 4)", result.GeneratedCode);
    }

    [Fact]
    public void Compile_unary_minus_with_multiplication()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print -1 * 2", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("-1 * 2", result.GeneratedCode);
    }

    [Fact]
    public void Compile_complex_precedence_chain()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1 + 2 * 3 - 4", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("1 + 2 * 3 - 4", result.GeneratedCode);
    }

    [Fact]
    public void Compile_nested_parentheses_deep()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print ((1 + 2) * 3) + 4", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("(1 + 2) * 3 + 4", result.GeneratedCode);
    }

    [Fact]
    public void Compile_division_and_subtraction_chain()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 10 / 2 - 3 * 4", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("10 / 2 - 3 * 4", result.GeneratedCode);
    }

    [Fact]
    public void Compile_comparison_preserves_precedence()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1 + 2 == 3", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("1 + 2 == 3", result.GeneratedCode);
    }

    [Fact]
    public void Compile_logical_preserves_precedence()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print true || false && true", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("true || false && true", result.GeneratedCode);
    }

    [Fact]
    public void Compile_float_literals_preserve_format()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 1.5 + 2.25", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("1.5 + 2.25", result.GeneratedCode);
    }
}
