using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class DiagnosticFormattingTests
{
    [Fact]
    public void Diagnostic_includes_token_text_when_available()
    {
        var sourceText = new SourceText("$", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains("found '$'", syntaxTree.Diagnostics[0].Message);
    }
}
