using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

public class LexerStringTests
{
    [Fact]
    public void String_literal_is_tokenized()
    {
        var sourceText = new SourceText("print \"hello\"", "test.lume");
        var lexer = new Lexer(sourceText);

        var tokens = new List<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        } while (token.Kind != TokenKind.EndOfFile);

        Assert.Equal(TokenKind.PrintKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.StringLiteral, tokens[1].Kind);
        Assert.Equal("hello", tokens[1].Value);
        Assert.Equal(TokenKind.EndOfFile, tokens[2].Kind);
    }

    [Fact]
    public void Unterminated_string_produces_diagnostic()
    {
        var sourceText = new SourceText("print \"oops", "test.lume");
        var lexer = new Lexer(sourceText);

        while (lexer.Lex().Kind != TokenKind.EndOfFile)
        {
        }

        Assert.NotEmpty(lexer.Diagnostics);
    }
}
