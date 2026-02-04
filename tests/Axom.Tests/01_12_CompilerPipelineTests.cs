using Axom.Compiler;
using Axom.Compiler.Syntax;

public class CompilerPipelineTests
{
    [Fact]
    public void Compile_print_string_generates_console_write()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"hello\"", "test.axom");

        Assert.True(result.Success);
        Assert.Empty(result.Diagnostics);
        Assert.Contains("Console.WriteLine(\"hello\");", result.GeneratedCode);

        var statement = Assert.IsType<PrintStatementSyntax>(result.SyntaxTree.Root.Statements.Single());
        var literal = Assert.IsType<LiteralExpressionSyntax>(statement.Expression);
        Assert.Equal("hello", literal.LiteralToken.Value);
    }

    [Fact]
    public void Compile_unterminated_string_fails()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"oops", "test.axom");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
        Assert.True(result.Diagnostics[0].Span.Length > 0);
    }

    [Fact]
    public void Compile_print_int_generates_console_write()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 42", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(42);", result.GeneratedCode);
    }

    [Fact]
    public void Compile_print_bool_generates_console_write()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print true", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(true);", result.GeneratedCode);
    }

    [Fact]
    public void Compile_variable_generates_var_declaration()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let x = 10", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("var x = 10;", result.GeneratedCode);
    }

    [Fact]
    public void Compile_variable_and_print_generates_code()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let x = 10\nprint x", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("var x = 10;", result.GeneratedCode);
        Assert.Contains("Console.WriteLine(x);", result.GeneratedCode);
    }

    [Fact]
    public void Compile_tuple_deconstruction_generates_code()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let (x, y) = (1, 2)\nprint x\nprint y", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("var x", result.GeneratedCode);
        Assert.Contains("var y", result.GeneratedCode);
        Assert.Contains("Item1", result.GeneratedCode);
        Assert.Contains("Item2", result.GeneratedCode);
    }

    [Fact]
    public void Compile_arithmetic_generates_expression()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print 2 + 3 * 4", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(2 + 3 * 4);", result.GeneratedCode);
    }

    [Fact]
    public void Compile_mutable_assignment_generates_code()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("let mut x = 1\nx = 2\nprint x", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("var x = 1;", result.GeneratedCode);
        Assert.Contains("x = 2;", result.GeneratedCode);
        Assert.Contains("Console.WriteLine(x);", result.GeneratedCode);
    }

    [Fact]
    public void Compile_block_generates_braces()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("{\n    let x = 1\n}", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("{", result.GeneratedCode);
        Assert.Contains("var x = 1;", result.GeneratedCode);
        Assert.Contains("}", result.GeneratedCode);
    }

    [Fact]
    public void Compile_unary_minus_generates_expression()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print -5", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(-5);", result.GeneratedCode);
    }

    [Fact]
    public void Compile_parenthesized_preserves_grouping()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print (2 + 3) * 4", "test.axom");

        Assert.True(result.Success);
        // Le parentesi devono essere preservate per mantenere la semantica corretta
        // (2 + 3) * 4 = 20, mentre 2 + 3 * 4 = 14
        Assert.Contains("(2 + 3) * 4", result.GeneratedCode);
    }
}
