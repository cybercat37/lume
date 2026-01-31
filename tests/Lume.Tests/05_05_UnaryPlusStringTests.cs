using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class UnaryPlusStringTests
{
    [Fact]
    public void Unary_plus_on_string_produces_diagnostic()
    {
        var sourceText = new SourceText("print +\"hi\"", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
