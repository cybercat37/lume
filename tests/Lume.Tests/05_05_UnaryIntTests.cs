using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class UnaryIntTests
{
    [Fact]
    public void Unary_minus_on_int_is_allowed()
    {
        var sourceText = new SourceText("print -1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
