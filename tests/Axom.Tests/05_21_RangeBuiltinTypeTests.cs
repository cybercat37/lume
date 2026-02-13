using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class RangeBuiltinTypeTests
{
    [Fact]
    public void Range_type_checks_for_valid_signature()
    {
        var sourceText = new SourceText("let xs = range(1, 4)\nprint xs", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Range_rejects_non_int_argument()
    {
        var sourceText = new SourceText("let xs = range(1, \"4\")", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }
}
