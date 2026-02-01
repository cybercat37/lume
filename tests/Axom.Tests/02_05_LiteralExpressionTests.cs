using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class LiteralExpressionTests
{
    [Fact]
    public void Numeric_literal_parses_as_literal_expression()
    {
        var sourceText = new SourceText("print 42", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var literal = Assert.IsType<LiteralExpressionSyntax>(statement.Expression);

        Assert.Equal(42, literal.LiteralToken.Value);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
