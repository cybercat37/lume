using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

public class PunctuationLexerTests
{
    [Fact]
    public void Punctuation_tokens_are_lexed()
    {
        var sourceText = new SourceText("let mut x = (1 + 2) * 3\nprint user.name", "test.axom");
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

        Assert.Contains(TokenKind.LetKeyword, tokens);
        Assert.Contains(TokenKind.MutKeyword, tokens);
        Assert.Contains(TokenKind.Identifier, tokens);
        Assert.Contains(TokenKind.EqualsToken, tokens);
        Assert.Contains(TokenKind.OpenParen, tokens);
        Assert.Contains(TokenKind.CloseParen, tokens);
        Assert.Contains(TokenKind.Plus, tokens);
        Assert.Contains(TokenKind.Star, tokens);
        Assert.Contains(TokenKind.Dot, tokens);
    }

    [Fact]
    public void Ellipsis_token_is_lexed()
    {
        var sourceText = new SourceText("let user = User { ...base }", "test.axom");
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

        Assert.Contains(TokenKind.Ellipsis, tokens);
    }

    [Fact]
    public void At_token_is_lexed()
    {
        var sourceText = new SourceText("@intent(\"io\") let x = 1", "test.axom");
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

        Assert.Contains(TokenKind.At, tokens);
    }
}
