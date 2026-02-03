using Axom.Compiler.Lexing;
using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class PrecedenceTests
{
    [Fact]
    public void Multiplication_binds_tighter_than_addition()
    {
        var sourceText = new SourceText("print 1 + 2 * 3", "test.axom");
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
        var sourceText = new SourceText("print -1 * 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var multiply = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.Star, multiply.OperatorToken.Kind);

        var left = Assert.IsType<UnaryExpressionSyntax>(multiply.Left);
        Assert.Equal(TokenKind.Minus, left.OperatorToken.Kind);
    }

    [Fact]
    public void Comparison_binds_looser_than_addition()
    {
        var sourceText = new SourceText("print 1 + 2 == 3", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var comparison = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.EqualEqual, comparison.OperatorToken.Kind);

        var left = Assert.IsType<BinaryExpressionSyntax>(comparison.Left);
        Assert.Equal(TokenKind.Plus, left.OperatorToken.Kind);
    }

    [Fact]
    public void And_binds_tighter_than_or()
    {
        var sourceText = new SourceText("print true || false && true", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var orExpression = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.PipePipe, orExpression.OperatorToken.Kind);

        var right = Assert.IsType<BinaryExpressionSyntax>(orExpression.Right);
        Assert.Equal(TokenKind.AmpersandAmpersand, right.OperatorToken.Kind);
    }

    [Fact]
    public void Comparison_binds_tighter_than_and()
    {
        var sourceText = new SourceText("print 1 < 2 && true", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statements.Single());
        var andExpression = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.AmpersandAmpersand, andExpression.OperatorToken.Kind);

        var left = Assert.IsType<BinaryExpressionSyntax>(andExpression.Left);
        Assert.Equal(TokenKind.Less, left.OperatorToken.Kind);
    }
}
