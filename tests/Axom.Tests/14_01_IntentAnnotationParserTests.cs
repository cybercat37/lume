using Axom.Compiler.Parsing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

public class IntentAnnotationParserTests
{
    [Fact]
    public void Intent_annotation_on_let_parses()
    {
        var sourceText = new SourceText("@intent(\"io\") let x = 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var let = Assert.IsType<VariableDeclarationSyntax>(syntaxTree.Root.Statements.Single());
        Assert.NotNull(let.IntentAnnotation);
        Assert.Equal("intent", let.IntentAnnotation!.IntentIdentifier.Text);
        Assert.Equal("io", let.IntentAnnotation.MessageToken.Value);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Intent_annotation_on_block_parses()
    {
        var sourceText = new SourceText("@intent(\"scope\") { print 1 }", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var block = Assert.IsType<BlockStatementSyntax>(syntaxTree.Root.Statements.Single());
        Assert.NotNull(block.IntentAnnotation);
        Assert.Equal("scope", block.IntentAnnotation!.MessageToken.Value);
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Intent_annotation_on_invalid_target_reports_diagnostic()
    {
        var sourceText = new SourceText("@intent(\"io\") print 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Diagnostics, d =>
            d.Message.Contains("can only be applied", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Unknown_annotation_reports_diagnostic()
    {
        var sourceText = new SourceText("@note(\"x\") let y = 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Diagnostics, d =>
            d.Message.Contains("Only @intent annotations are supported", StringComparison.OrdinalIgnoreCase)
            || d.Message.Contains("does not take arguments", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Logging_aspect_keyword_on_function_parses()
    {
        var sourceText = new SourceText("@logging fn add(a: Int, b: Int) -> Int => a + b", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var function = Assert.IsType<FunctionDeclarationSyntax>(syntaxTree.Root.Statements.Single());
        Assert.Equal("logging", Assert.Single(function.Aspects));
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Timeout_aspect_keyword_on_function_parses()
    {
        var sourceText = new SourceText("@timeout(200) fn load(max: Int) => rand_int(max)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var function = Assert.IsType<FunctionDeclarationSyntax>(syntaxTree.Root.Statements.Single());
        Assert.Equal("timeout:200", Assert.Single(function.Aspects));
        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Timeout_aspect_without_argument_reports_diagnostic()
    {
        var sourceText = new SourceText("@timeout fn load(max: Int) => rand_int(max)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Contains(syntaxTree.Diagnostics, d =>
            d.Message.Contains("@timeout requires a positive integer argument", StringComparison.OrdinalIgnoreCase));
    }
}
