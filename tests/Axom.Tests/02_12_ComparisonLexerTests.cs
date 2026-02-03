using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

public class ComparisonLexerTests
{
    [Fact]
    public void Comparison_tokens_are_lexed()
    {
        var sourceText = new SourceText("1 == 2 != 3 < 4 <= 5 > 6 >= 7", "test.axom");
        var lexer = new Lexer(sourceText);

        var tokens = new List<TokenKind>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            if (token.Kind != TokenKind.BadToken)
            {
                tokens.Add(token.Kind);
            }
        } while (token.Kind != TokenKind.EndOfFile);

        Assert.Contains(TokenKind.EqualEqual, tokens);
        Assert.Contains(TokenKind.BangEqual, tokens);
        Assert.Contains(TokenKind.Less, tokens);
        Assert.Contains(TokenKind.LessOrEqual, tokens);
        Assert.Contains(TokenKind.Greater, tokens);
        Assert.Contains(TokenKind.GreaterOrEqual, tokens);
    }
}
