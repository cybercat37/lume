using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class NestedParenEofTests
{
    [Fact]
    public void Missing_nested_close_paren_produces_diagnostic()
    {
        var sourceText = new SourceText("print ((1 + 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
