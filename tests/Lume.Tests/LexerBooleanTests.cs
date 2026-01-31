using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

public class LexerBooleanTests
{
    [Fact]
    public void Boolean_keywords_are_tokenized()
    {
        var sourceText = new SourceText("print true", "test.lume");
        var lexer = new Lexer(sourceText);

        var tokens = new List<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        } while (token.Kind != TokenKind.EndOfFile);

        Assert.Equal(TokenKind.PrintKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.TrueKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.EndOfFile, tokens[^1].Kind);
    }
}
