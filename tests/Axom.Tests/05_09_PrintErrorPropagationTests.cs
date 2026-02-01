using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PrintErrorPropagationTests
{
    [Fact]
    public void Print_of_invalid_expression_produces_single_diagnostic()
    {
        var sourceText = new SourceText("print x", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Single(result.Diagnostics);
    }
}
