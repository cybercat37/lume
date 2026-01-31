using Lume.Compiler.Binding;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class PrintErrorPropagationTests
{
    [Fact]
    public void Print_of_invalid_expression_produces_single_diagnostic()
    {
        var sourceText = new SourceText("print x", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Single(result.Diagnostics);
    }
}
