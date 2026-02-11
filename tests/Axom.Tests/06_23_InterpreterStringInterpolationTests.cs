using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterStringInterpolationTests
{
    [Fact]
    public void Print_interpolated_string_outputs_rendered_values()
    {
        var sourceText = new SourceText("let n = 7\nlet ok = true\nprint f\"n={n}, ok={ok}\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("n=7, ok=true", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Interpolated_string_supports_escaped_braces()
    {
        var sourceText = new SourceText("print f\"{{value}}\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("{value}", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Interpolated_string_applies_numeric_format_specifier()
    {
        var sourceText = new SourceText("let n = 7\nprint f\"n={n:000}\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("n=007", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Interpolated_string_applies_string_format_specifier()
    {
        var sourceText = new SourceText("let name = \"ada\"\nprint f\"{name:upper}\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("ADA", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
