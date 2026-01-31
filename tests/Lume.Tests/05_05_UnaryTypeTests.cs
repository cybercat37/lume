using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class UnaryTypeTests
{
    [Fact]
    public void Unary_minus_on_bool_produces_diagnostic()
    {
        var sourceText = new SourceText("print -true", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
