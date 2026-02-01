using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ErrorTypePropagationTests
{
    [Fact]
    public void Error_type_suppresses_cascading_diagnostics()
    {
        var sourceText = new SourceText("print x + 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Single(result.Diagnostics);
    }
}
