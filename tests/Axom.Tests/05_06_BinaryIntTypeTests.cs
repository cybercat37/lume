using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class BinaryIntTypeTests
{
    [Fact]
    public void Binary_addition_on_ints_is_allowed()
    {
        var sourceText = new SourceText("print 1 + 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
