using Lume.Compiler.Lexing;
using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

public class ParserUnaryPrecedenceTests
{
    [Fact]
    public void Unary_binds_tighter_than_multiplication()
    {
        var sourceText = new SourceText("print -1 * 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var statement = Assert.IsType<PrintStatementSyntax>(syntaxTree.Root.Statement);
        var multiply = Assert.IsType<BinaryExpressionSyntax>(statement.Expression);

        Assert.Equal(TokenKind.Star, multiply.OperatorToken.Kind);

        var left = Assert.IsType<UnaryExpressionSyntax>(multiply.Left);
        Assert.Equal(TokenKind.Minus, left.OperatorToken.Kind);
    }
}
