using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class ParserSqlLiteralTests
{
    [Fact]
    public void Sql_triple_quoted_literal_parses_as_string_expression()
    {
        var sourceText = new SourceText("print sql\"\"\"select 1\"\"\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements[0]);
        _ = Assert.IsType<LiteralExpressionSyntax>(statement.Expression);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Sql_triple_quoted_literal_can_span_multiple_lines()
    {
        var sourceText = new SourceText("print sql\"\"\"select\n  1\nfrom dual\"\"\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Unterminated_sql_triple_quoted_literal_reports_diagnostic()
    {
        var sourceText = new SourceText("print sql\"\"\"select 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("Unterminated sql string literal", StringComparison.Ordinal));
    }
}
