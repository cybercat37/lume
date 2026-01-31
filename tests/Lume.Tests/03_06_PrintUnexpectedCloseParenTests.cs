using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class PrintUnexpectedCloseParenTests
{
    [Fact]
    public void Unexpected_close_paren_produces_diagnostic()
    {
        var sourceText = new SourceText("print )", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
