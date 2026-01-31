using Lume.Compiler.Lexing;
using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class ParserPrecedenceTests
{
    [Fact]
    public void Multiplication_binds_tighter_than_addition()
    {
        var sourceText = new SourceText("print 1 + 2 * 3", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statement);
        var addition = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.Plus, addition.OperatorToken.Kind);

        var right = Assert.IsType<BinaryExpressionSyntax>(addition.Right);
        Assert.Equal(TokenKind.Star, right.OperatorToken.Kind);
    }
}
