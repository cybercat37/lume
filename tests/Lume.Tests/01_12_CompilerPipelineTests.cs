using Lume.Compiler;
using Lume.Compiler.Syntax;

public class CompilerPipelineTests
{
    [Fact]
    public void Compile_print_string_generates_console_write()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("print \"hello\"", "test.lume");

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
        var result = compiler.Compile("print \"oops", "test.lume");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
        Assert.True(result.Diagnostics[0].Span.Length > 0);
    }
}
