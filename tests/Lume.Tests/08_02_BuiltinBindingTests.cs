using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class BuiltinBindingTests
{
    [Fact]
    public void Builtin_input_resolves_without_diagnostic()
    {
        var sourceText = new SourceText("print input", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
