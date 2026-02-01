using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class BuiltinBindingTests
{
    [Fact]
    public void Builtin_input_resolves_without_diagnostic()
    {
        var sourceText = new SourceText("print input", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
