using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class BinaryTypeTests
{
    [Fact]
    public void Binary_addition_on_string_and_int_produces_diagnostic()
    {
        var sourceText = new SourceText("print \"a\" + 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
