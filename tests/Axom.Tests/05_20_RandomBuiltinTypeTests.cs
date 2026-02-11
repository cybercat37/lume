using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class RandomBuiltinTypeTests
{
    [Fact]
    public void Random_builtins_type_check_for_valid_signatures()
    {
        var sourceText = new SourceText("rand_seed(7)\nlet a = rand_float()\nlet b = rand_int(10)\nsleep(1)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Rand_int_rejects_non_int_argument()
    {
        var sourceText = new SourceText("let x = rand_int(\"10\")", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }
}
