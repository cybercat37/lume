using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class DiagnosticMessageTests
{
    [Fact]
    public void Missing_token_diagnostic_includes_expected_and_found()
    {
        var sourceText = new SourceText("let = 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
        Assert.Contains("Expected identifier", syntaxTree.Diagnostics[0].Message);
        Assert.Contains("found", syntaxTree.Diagnostics[0].Message);
    }
}
