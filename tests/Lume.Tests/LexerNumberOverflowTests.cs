using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

public class LexerNumberOverflowTests
{
    [Fact]
    public void Number_literal_overflow_produces_diagnostic()
    {
        var sourceText = new SourceText("print 9999999999999999999999", "test.lume");
        var lexer = new Lexer(sourceText);

        while (lexer.Lex().Kind != TokenKind.EndOfFile)
        {
        }

        Assert.NotEmpty(lexer.Diagnostics);
    }
}
