using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ListWindowBuiltinTypeTests
{
    [Fact]
    public void Take_skip_while_enumerate_zip_type_check_for_valid_signatures()
    {
        var sourceText = new SourceText(
            "let a = take([1, 2, 3], 2)\nlet b = skip([1, 2, 3], 1)\nlet c = take_while([1, 2, 3], fn(x: Int) => x < 3)\nlet d = skip_while([1, 2, 3], fn(x: Int) => x < 3)\nlet e = enumerate([10, 20])\nlet f = zip([1, 2], [\"a\", \"b\"])\nlet g = zip_with([1, 2], [10, 20], fn(x: Int, y: Int) => x + y)",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Take_rejects_non_int_count_argument()
    {
        var sourceText = new SourceText("let x = take([1, 2, 3], \"2\")", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Zip_rejects_non_list_argument()
    {
        var sourceText = new SourceText("let x = zip([1, 2], 3)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Zip_with_rejects_wrong_combiner_shape()
    {
        var sourceText = new SourceText("let x = zip_with([1, 2], [3, 4], fn(v: Int) => v)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Take_while_rejects_non_bool_predicate()
    {
        var sourceText = new SourceText("let x = take_while([1, 2, 3], fn(v: Int) => v + 1)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Enumerate_rejects_non_list_argument()
    {
        var sourceText = new SourceText("let x = enumerate(123)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }
}
