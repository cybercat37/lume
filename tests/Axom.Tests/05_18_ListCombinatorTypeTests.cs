using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ListCombinatorTypeTests
{
    [Fact]
    public void Map_with_typed_lambda_binds()
    {
        var sourceText = new SourceText("print map([1, 2, 3], fn(x: Int) => x * 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Fold_with_typed_lambda_binds()
    {
        var sourceText = new SourceText("print fold([1, 2, 3], 0, fn(acc: Int, x: Int) => acc + x)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Map_lambda_parameter_mismatch_produces_diagnostic()
    {
        var sourceText = new SourceText("print map([1, 2, 3], fn(x: String) => x)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Map_with_shorthand_lambda_binds()
    {
        var sourceText = new SourceText("print map([1, 2, 3], x -> x * 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Shorthand_lambda_without_context_produces_diagnostic()
    {
        var sourceText = new SourceText("let f = x -> x * 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
