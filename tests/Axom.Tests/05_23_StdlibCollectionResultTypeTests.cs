using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class StdlibCollectionResultTypeTests
{
    [Fact]
    public void Count_sum_any_all_result_map_type_check_for_valid_signatures()
    {
        var sourceText = new SourceText(
            "let a = count([1, 2, 3])\nlet b = sum([1, 2, 3])\nlet c = sum([1.0, 2.5])\nlet d = any([1, 2, 3], fn(x: Int) => x > 2)\nlet e = all([1, 2, 3], fn(x: Int) => x > 0)\nlet f = result_map(rand_int(10), fn(x: Int) => x + 1)",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Sum_rejects_non_numeric_list()
    {
        var sourceText = new SourceText("let x = sum([\"a\", \"b\"])", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Result_map_rejects_non_result_value()
    {
        var sourceText = new SourceText("let x = result_map(1, fn(v: Int) => v + 1)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }
}
