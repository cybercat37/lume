using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class ParserExpressionTests
{
    [Fact]
    public void Numeric_literal_parses_as_literal_expression()
    {
        var sourceText = new SourceText("print 42", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statement);
        var literal = Assert.IsType<LiteralExpressionSyntax>(statement.Expression);

        Assert.Equal(42, literal.LiteralToken.Value);
        Assert.Empty(syntaxTree.Diagnostics);
    }
}
