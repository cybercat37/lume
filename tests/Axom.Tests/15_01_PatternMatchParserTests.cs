using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PatternMatchParserTests
{
    [Fact]
    public void Match_expression_parses()
    {
        var sourceText = new SourceText(@"
let result = match 2 {
  1 -> ""one""
  2 -> ""two""
  _ -> ""many""
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Match_missing_arrow_produces_diagnostic()
    {
        var sourceText = new SourceText(@"
let result = match 1 {
  0 ""zero""
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.NotEmpty(syntaxTree.Diagnostics);
    }
}
