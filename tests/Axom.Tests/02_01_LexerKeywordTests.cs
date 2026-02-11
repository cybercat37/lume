using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

public class LexerKeywordTests
{
    [Fact]
    public void Boolean_keywords_are_tokenized()
    {
        var sourceText = new SourceText("print true", "test.axom");
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

    [Fact]
    public void Type_keyword_is_tokenized()
    {
        var sourceText = new SourceText("type User { name: String }", "test.axom");
        var lexer = new Lexer(sourceText);

        var tokens = new List<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        } while (token.Kind != TokenKind.EndOfFile);

        Assert.Equal(TokenKind.TypeKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.EndOfFile, tokens[^1].Kind);
    }

    [Fact]
    public void Module_keywords_are_tokenized()
    {
        var sourceText = new SourceText("pub import from as", "test.axom");
        var lexer = new Lexer(sourceText);

        var tokens = new List<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        } while (token.Kind != TokenKind.EndOfFile);

        Assert.Equal(TokenKind.PubKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.ImportKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.FromKeyword, tokens[2].Kind);
        Assert.Equal(TokenKind.AsKeyword, tokens[3].Kind);
        Assert.Equal(TokenKind.EndOfFile, tokens[^1].Kind);
    }

    [Fact]
    public void With_keyword_is_tokenized()
    {
        var sourceText = new SourceText("with", "test.axom");
        var lexer = new Lexer(sourceText);

        var token = lexer.Lex();

        Assert.Equal(TokenKind.WithKeyword, token.Kind);
    }

}
