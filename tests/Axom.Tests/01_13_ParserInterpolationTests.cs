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
}
