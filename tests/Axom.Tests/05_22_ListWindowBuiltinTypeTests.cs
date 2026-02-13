using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class ListWindowBuiltinTypeTests
{
    [Fact]
    public void Take_skip_zip_type_check_for_valid_signatures()
    {
        var sourceText = new SourceText(
            "let a = take([1, 2, 3], 2)\nlet b = skip([1, 2, 3], 1)\nlet c = zip([1, 2], [\"a\", \"b\"])",
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
}
