using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class ParserDiagnosticTests
{
    [Fact]
    public void Missing_close_paren_produces_diagnostic()
    {
        var sourceText = new SourceText("print (1 + 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
