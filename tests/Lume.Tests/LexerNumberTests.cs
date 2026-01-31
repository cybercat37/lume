using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

public class LexerNumberTests
{
    [Fact]
    public void Number_literal_is_tokenized()
    {
        var sourceText = new SourceText("print 123", "test.lume");
        var lexer = new Lexer(sourceText);

        var tokens = new List<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        } while (token.Kind != TokenKind.EndOfFile);

        Assert.Equal(TokenKind.PrintKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.NumberLiteral, tokens[1].Kind);
        Assert.Equal(123, tokens[1].Value);
        Assert.Equal(TokenKind.EndOfFile, tokens[^1].Kind);
    }
}
