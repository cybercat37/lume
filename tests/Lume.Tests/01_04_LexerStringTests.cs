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

    [Fact]
    public void Identifier_with_underscore_is_tokenized()
    {
        // Gli underscore devono essere supportati negli identificatori
        // my_var, _private, camelCase_with_underscore sono tutti validi
        var sourceText = new SourceText("my_var", "test.lume");
        var lexer = new Lexer(sourceText);

        var token = lexer.Lex();

        Assert.Equal(TokenKind.Identifier, token.Kind);
        Assert.Equal("my_var", token.Text);
        Assert.Empty(lexer.Diagnostics);
    }

    [Fact]
    public void Identifier_starting_with_underscore_is_tokenized()
    {
        var sourceText = new SourceText("_private", "test.lume");
        var lexer = new Lexer(sourceText);

        var token = lexer.Lex();

        Assert.Equal(TokenKind.Identifier, token.Kind);
        Assert.Equal("_private", token.Text);
        Assert.Empty(lexer.Diagnostics);
    }
}
