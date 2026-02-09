using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class FunctionParserTests
{
    [Fact]
    public void Function_block_body_parses()
    {
        var sourceText = new SourceText(@"
fn add(x: Int, y: Int) {
  x + y
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Function_expression_body_parses()
    {
        var sourceText = new SourceText("fn add(x: Int, y: Int) => x + y", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Lambda_expression_parses()
    {
        var sourceText = new SourceText("let f = fn(x: Int) => x + 1", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Return_statement_parses()
    {
        var sourceText = new SourceText(@"
fn id(x: Int) {
  return x
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Missing_parameter_type_produces_diagnostic()
    {
        var sourceText = new SourceText("fn add(x, y: Int) { x + y }", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Channel_expression_parses()
    {
        var sourceText = new SourceText("let pair = channel<Int>()", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Channel_expression_with_capacity_parses()
    {
        var sourceText = new SourceText("let pair = channel<Int>(8)", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

}
