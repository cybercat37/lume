using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class TimeStdlibTypeTests
{
    [Fact]
    public void Time_builtins_type_check_for_valid_signatures()
    {
        var sourceText = new SourceText(
            "let now = time_now_utc()\nlet later = time_add_ms(now, 500)\nlet diff = time_diff_ms(later, now)\nlet iso = time_to_iso(now)\nlet localIso = time_to_local_iso(now)\nlet parsed = time_from_iso(iso)",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Time_add_ms_rejects_non_instant_argument()
    {
        var sourceText = new SourceText("let x = time_add_ms(1, 2)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Time_from_iso_rejects_non_string_argument()
    {
        var sourceText = new SourceText("let x = time_from_iso(123)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }
}
