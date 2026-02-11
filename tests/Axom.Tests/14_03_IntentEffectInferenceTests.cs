using Axom.Compiler;
using Axom.Compiler.Binding;
using Axom.Compiler.Diagnostics;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class IntentEffectInferenceTests
{
    [Fact]
    public void Intent_does_not_emit_warning_for_missing_dotnet_effect()
    {
        var sourceText = new SourceText("@intent(\"io\") { dotnet.call<Int>(\"System.Math\", \"Max\", 1, 2) }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Intent_does_not_emit_warning_for_missing_concurrency_effect()
    {
        var sourceText = new SourceText("@intent(\"io\") let pair = channel<Int>()", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Intent_with_covered_effects_does_not_report_warning()
    {
        var sourceText = new SourceText("@intent(\"io,dotnet\") { print dotnet.call<Int>(\"System.Math\", \"Max\", 1, 2) }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Compiler_succeeds_without_intent_warning()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("@intent(\"io\") { dotnet.call<Int>(\"System.Math\", \"Max\", 1, 2) }", "test.axom");

        Assert.True(result.Success);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    }
}
