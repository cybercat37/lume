using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class SyntaxTreeAlwaysExistsTests
{
    [Fact]
    public void Syntax_tree_is_created_even_with_errors()
    {
        var sourceText = new SourceText(") @ let = ", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotNull(syntaxTree.Root);
        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
