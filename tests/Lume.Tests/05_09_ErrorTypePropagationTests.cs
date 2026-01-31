using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class ErrorTypePropagationTests
{
    [Fact]
    public void Error_type_suppresses_cascading_diagnostics()
    {
        var sourceText = new SourceText("print x + 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Single(result.Diagnostics);
    }
}
