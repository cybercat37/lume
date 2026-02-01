using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PrintUnexpectedCloseParenTests
{
    [Fact]
    public void Unexpected_close_paren_produces_diagnostic()
    {
        var sourceText = new SourceText("print )", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
