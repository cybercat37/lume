using Lume.Compiler.Lexing;
using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class PrecedenceTests
{
    [Fact]
    public void Multiplication_binds_tighter_than_addition()
    {
        var sourceText = new SourceText("print 1 + 2 * 3", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var addition = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.Plus, addition.OperatorToken.Kind);

        var right = Assert.IsType<BinaryExpressionSyntax>(addition.Right);
        Assert.Equal(TokenKind.Star, right.OperatorToken.Kind);
    }

    [Fact]
    public void Unary_binds_tighter_than_multiplication()
    {
        var sourceText = new SourceText("print -1 * 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var multiply = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.Star, multiply.OperatorToken.Kind);

        var left = Assert.IsType<UnaryExpressionSyntax>(multiply.Left);
        Assert.Equal(TokenKind.Minus, left.OperatorToken.Kind);
    }
}
