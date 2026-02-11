using Axom.Compiler.Binding;
using Axom.Compiler.Lowering;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class IntentAnnotationBindingTests
{
    [Fact]
    public void Intent_annotation_on_let_binds_effect_tags()
    {
        var sourceText = new SourceText("@intent(\"io, dotnet\") let x = 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclaration>(result.Program.Statements.Single());
        Assert.NotNull(declaration.IntentAnnotation);
        Assert.Equal(new[] { "dotnet", "io" }, declaration.IntentAnnotation!.Effects.OrderBy(x => x));
    }

    [Fact]
    public void Empty_intent_message_reports_diagnostic()
    {
        var sourceText = new SourceText("@intent(\"\") let x = 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("at least one effect tag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Invalid_intent_tag_reports_diagnostic()
    {
        var sourceText = new SourceText("@intent(\"io, !bad\") let x = 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("Invalid intent effect tag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Intent_annotation_flows_to_lowered_statements()
    {
        var sourceText = new SourceText("@intent(\"io\") { print 1 }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var bindResult = binder.Bind(syntaxTree);
        Assert.Empty(bindResult.Diagnostics);

        var lowerer = new Lowerer();
        var lowered = lowerer.Lower(bindResult.Program);

        var block = Assert.IsType<LoweredBlockStatement>(lowered.Statements.Single());
        Assert.NotNull(block.IntentAnnotation);
        Assert.Equal("io", Assert.Single(block.IntentAnnotation!.Effects));
    }
}
