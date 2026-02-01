using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class UnaryTypeTests
{
    [Fact]
    public void Unary_minus_on_bool_produces_diagnostic()
    {
        var sourceText = new SourceText("print -true", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
