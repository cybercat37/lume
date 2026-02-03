using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Compiler.Binding;

public class ComparisonTypeTests
{
    [Fact]
    public void Equality_on_strings_is_allowed()
    {
        var sourceText = new SourceText("print \"a\" == \"b\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Comparison_on_ints_is_allowed()
    {
        var sourceText = new SourceText("print 1 < 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Comparison_between_mismatched_types_is_rejected()
    {
        var sourceText = new SourceText("print 1 == true", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
