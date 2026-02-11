using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PipelineTypeTests
{
    [Fact]
    public void Pipeline_into_function_is_allowed()
    {
        var sourceText = new SourceText("print 1 |> abs", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Pipeline_into_call_prepends_left_argument()
    {
        var sourceText = new SourceText("print 1 |> max(2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Pipeline_into_non_callable_expression_produces_diagnostic()
    {
        var sourceText = new SourceText("print 1 |> 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
