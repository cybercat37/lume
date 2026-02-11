using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class ParserInterpolationTests
{
    [Fact]
    public void Interpolated_string_parses_as_expression()
    {
        var sourceText = new SourceText("let name = \"world\"\nprint f\"hello {name}\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements[1]);
        _ = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Interpolated_string_with_format_specifier_parses()
    {
        var sourceText = new SourceText("let n = 7\nprint f\"n={n:000}\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Unterminated_interpolation_reports_diagnostic_position()
    {
        var sourceText = new SourceText("print f\"value={1 + 2\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var diagnostic = Assert.Single(syntaxTree.Diagnostics.Where(d =>
            d.Message.Contains("not closed", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(1, diagnostic.Line);
        Assert.True(diagnostic.Column > 1);
    }
}
