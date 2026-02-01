using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class UnaryPlusStringTests
{
    [Fact]
    public void Unary_plus_on_string_produces_diagnostic()
    {
        var sourceText = new SourceText("print +\"hi\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
