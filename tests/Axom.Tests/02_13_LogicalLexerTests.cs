using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

public class LogicalLexerTests
{
    [Fact]
    public void Logical_tokens_are_lexed()
    {
        var sourceText = new SourceText("true && false || !true", "test.axom");
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

        Assert.Contains(TokenKind.AmpersandAmpersand, tokens);
        Assert.Contains(TokenKind.PipePipe, tokens);
        Assert.Contains(TokenKind.Bang, tokens);
    }
}
