using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class BooleanLiteralTests
{
    [Fact]
    public void Boolean_literal_parses_as_literal_expression()
    {
        var sourceText = new SourceText("print true", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var literal = Assert.IsType<LiteralExpressionSyntax>(statement.Expression);

        Assert.Equal(true, literal.LiteralToken.Value);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
