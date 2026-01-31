using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class BuiltinArityTests
{
    [Fact]
    public void Builtin_with_extra_argument_produces_diagnostic()
    {
        var sourceText = new SourceText("println 1 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
