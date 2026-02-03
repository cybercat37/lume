using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class SumTypeParserTests
{
    [Fact]
    public void Sum_type_declaration_parses()
    {
        var sourceText = new SourceText(@"
type Result {
  Ok(Int)
  Error(String)
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Sum_type_allows_payloadless_variants()
    {
        var sourceText = new SourceText(@"
type Status {
  Ready
  Error(String)
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }

    [Fact]
    public void Sum_type_allows_trailing_commas()
    {
        var sourceText = new SourceText(@"
type Result {
  Ok(Int),
  Error(String),
}
", "test.axom");

        var syntaxTree = SyntaxTree.Parse(sourceText);

        Assert.Empty(syntaxTree.Diagnostics);
    }
}
