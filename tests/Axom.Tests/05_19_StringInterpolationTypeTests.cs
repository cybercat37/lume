using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class StringInterpolationTypeTests
{
    [Fact]
    public void Interpolated_string_accepts_non_string_expressions()
    {
        var sourceText = new SourceText("let n = 7\nprint f\"n={n}\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }
}
