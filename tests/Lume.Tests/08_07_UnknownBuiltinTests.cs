using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class UnknownBuiltinTests
{
    [Fact]
    public void Unknown_builtin_produces_diagnostic()
    {
        var sourceText = new SourceText("foo 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
