using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class BinaryBoolTypeTests
{
    [Fact]
    public void Binary_addition_on_bools_produces_diagnostic()
    {
        var sourceText = new SourceText("print true + false", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
